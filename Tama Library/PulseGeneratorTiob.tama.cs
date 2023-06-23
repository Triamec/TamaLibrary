// Copyright © 2014 Triamec Motion AG

/* Overview
 * --------
 * This pulse generator Tama program is able to output periodical pulses at TIOB outputs 1..8.
 * The program is configurable via Tama application parameters. 
 * Note: This program is used by the testing framework. 
 * 
 * Inputs:
 * GenPurposeVar0:         Pulse period [s]
 * GenPurposeVar1:         Pulse width [s]. If greater than or equal to Pulse period, an IndexOutOfRange error occurs.
 * GenPurposeIntVar0:      A bit combination identifying the outputs to use. (B0..B7: OutputBits1..8)
 * GenPurposeIntVar1:      Negative logic: Set to 0 if pulse is to be high. Set to 1 if pulse is to be low.
 * IsochronousMainCommand: Set to something different than 0 in order to start.
 * 
 * Outputs:
 * IsochronousMainState:   Set to the value of IsochronousMainCommand
 * OutputBits:			   Output for generated pulses.
 *
 * example of pulse output sequence (output1&2 for 0.5ms, every 200ms)
 * pulse period = 2*200ms = 400ms, pulse width 0.5ms, pulse pattern 0x3
 * ____                 _____           _____
 *     |_______________|  01 |_________|  02 |_____ 
 *     |--  200ms   -->|--  200ms   -->| 
 *                     |0.5ms|         |0.5ms| 
 *
 * Notes:
 * While the digital outputs which are not configured to be pulsed are left untouched, the program must have exclusive
 * access to the outputs while pulsing. Otherwise, the state of other digital outputs is undefined.
 */
using System.Diagnostics.CodeAnalysis;
using Triamec.Tam.Samples;
using Triamec.Tama.Rlid16;  // TIOB
using Triamec.Tama.Vmid5;

/// <summary>
/// Tama program for the I/O adapters to generate pulses on one or several outputs.
/// </summary>
[Tama]
#pragma warning disable CA1812 // internal unused class
static class PulseGeneratorTiob {
	#region Constants
	/// <summary>
	/// Number of available output bits.
	/// <para>The value is 8.</para>
	/// </summary>
	const int OutputCount = 8;

	#endregion Constants

	#region Fields
	/// <summary>
	/// Timer triggering raising and falling edges.
	/// </summary>
	static readonly IsochronousTimer Timer =
		new IsochronousTimer(Register.General.Parameters.CycleTimePathPlanner);

	/// <summary>
	/// Precalculated pulse output masks.
	/// </summary>
	static readonly int[] OutputMasks = new int[OutputCount];

	/// <summary>
	/// Number of used digital outputs.
	/// </summary>
	static int _outputCount;

	/// <summary>
	/// Duration, in seconds, from one pulse to the next.
	/// </summary>
	static float _pulseStep;

	/// <summary>
	/// Duration, in seconds, from a pulse raise to the nearest pulse fall.
	/// </summary>
	static float _offDelay;

	/// <summary>
	/// Number of outputs to decrement from <see cref="_outputIndex"/> to get the index of the output to switch off.
	/// </summary>
	static int _offLag;

	/// <summary>
	/// Whether the next timer triggers a falling edge.
	/// </summary>
	static bool _nextTimerIsOff;

	/// <summary>
	/// The index of the currently pulsed output.
	/// </summary>
	static int _outputIndex = -1;

	/// <summary>
	/// The value of an unset output.
	/// </summary>
	static int _off;

	#endregion Fields

	#region Helpers
	/// <summary>
	/// Sets the specified output.
	/// </summary>
	/// <param name="index">The bit, by means of the <see cref="OutputMasks"/> array.</param>
	/// <param name="value"><c>1</c> to switch on, <c>0</c> to switch off.</param>
	/// <remarks>
	/// This method assumes exclusive access to the output bits. Bits other than the configured output bits will
	/// be left untouched only if they are static.
	/// </remarks>
	static void SetDigitalOutput(int index, int value) {
		if (value > 0) {
			Register.General.Commands.OutputBits = Register.General.Commands.OutputBits | OutputMasks[index];
		} else {
			Register.General.Commands.OutputBits = Register.General.Commands.OutputBits & ~OutputMasks[index];
		}
	}

	/// <summary>
	/// Starts to pulse.
	/// </summary>
	static void Pulse() {
		_outputIndex = (++_outputIndex) % _outputCount;
		SetDigitalOutput(_outputIndex, 1 - _off);
		Timer.Start(_offDelay);
		_nextTimerIsOff = true;
	}
	#endregion Helpers

	#region Tasks
	[TamaTask(Task.IsochronousMain)]
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Tama entry point")]
	static void IsoMain() {

		// main pulse sequencer
		if ((_outputCount > 0) && Timer.Tick()) {
			if (_nextTimerIsOff) {
				int offBit = (_outputCount + _outputIndex - _offLag) % _outputCount;
				SetDigitalOutput(offBit, _off);
				Timer.Start(_pulseStep - _offDelay);
				_nextTimerIsOff = false;
			} else {
				Pulse();
			}
		}

		// sync pulse outputs
		var command = Register.Tama.IsochronousMainCommand;
		if (Register.Tama.IsochronousMainState != command) {
			Register.Tama.IsochronousMainState = command;

			_nextTimerIsOff = false;
			_outputIndex = -1;

			if (command == 0) {
				// switched off, tear down current pulse sequence
				for (int i = 0; i < _outputCount; i++) {
					SetDigitalOutput(i, _off);
				}
				_outputCount = 0;
			} else {
				// switched on, set masks and calculate number of bits
				var outputs = Register.Tama.Variables.GenPurposeIntVar0;
				var j = 0;
				for (var i = 0; i < OutputCount; i++) {
					var mask = 1 << i;
					if ((mask & outputs) != 0) {
						OutputMasks[j++] = mask;
					}
				}
				_outputCount = j;

				if (_outputCount > 0) {
					// set initial state 
					_off = Register.Tama.Variables.GenPurposeIntVar1;
					for (int i = 0; i < _outputCount; i++) {
						SetDigitalOutput(i, _off);
					}

					// calculate pulse characteristics
					float pulsePeriod = Register.Tama.Variables.GenPurposeVar0;
					float pulseWidth = Register.Tama.Variables.GenPurposeVar1;
					if (pulseWidth >= pulsePeriod) {

						// provoke index out of range
						OutputMasks[int.MaxValue] = 0;
					}
					_pulseStep = pulsePeriod / _outputCount;
					_offDelay = pulseWidth % _pulseStep;
					_offLag = (int)(pulseWidth / _pulseStep);

					// start first pause 
					Timer.Start(_offDelay);
					_nextTimerIsOff = true;
				}
			}
		}
	}
	#endregion Tasks
}

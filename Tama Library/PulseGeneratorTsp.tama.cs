// Copyright © 2020 Triamec Motion AG

/* Overview
 * --------
 * This pulse generator Tama program is able to output periodical pulses at TSP Ax0 DigOutputs 1..4.
 * The program is configurable via Tama application parameters. This program is based on the implementation 
 * of PulsGenerator.tama.cs 
 * Note: This program is used by the testing framework. 
 * 
 * Inputs:
 * Application.Variables.Floats[0]:   Pulse period [s]
 * Application.Variables.Floats[1]:   Pulse width [s]. If greater than or equal to Pulse period, an IndexOutOfRange error occurs.
 * Application.Variables.Integers[0]: A bit combination identifying the outputs to use. 
 *									  (B0..B3: Ax0:DigOut1..4)
 * Application.Variables.Integers[1]: Negative logic: Set to 0 if pulse is to be high. Set to 1 if pulse is to be low.
 * IsochronousMainCommand: Set to something different than 0 in order to start.
 * 
 * Outputs:
 * IsochronousMainState:   Set to the value of IsochronousMainCommand
 * OutputBits:			   Output for generated pulses.
 * 
 * example of pulse output sequence (output1&2 on Axis0 for 0.5ms, every 200ms)
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
using Triamec.Tama.Rlid19;	// TSDxx
using Triamec.Tama.Vmid5;

/// <summary>
/// Tama program for the TSD drive to generate pulses on one or several axis outputs.
/// </summary>
[Tama]
static class PulseGeneratorTsp {
	#region Constants
	/// <summary>
	/// Number of supported axes.
	/// <para>The value is 1.</para>
	/// </summary>
	const int AxesCount = 1;
	/// <summary>
	/// Number of supported outputs per axis
	/// <para>The value is 4.</para>
	/// </summary>
	const int OutputsPerAxis = 4;
	/// <summary>
	/// Number of supported outputs (2 per axis).
	/// <para>The value is 4.</para>
	/// </summary>
	const int OutputCount = AxesCount * OutputsPerAxis;

	#endregion Constants

	#region Fields
	/// <summary>
	/// Timer triggering raising and falling edges.
	/// </summary>
	static readonly IsochronousTimer Timer =
		new IsochronousTimer(Register.General.Signals.Internals.CycleTimePathPlanner);

	/// <summary>
	/// zero based index of used axis per output.
	/// </summary>
	static readonly int[] OutputAxisIdx = new int[OutputCount];
	/// <summary>
	/// zero based index of used OutPin per output.
	/// </summary>
	static readonly int[] OutputPinIdx = new int[OutputCount];

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
	/// Number of outputs to decrement from <see cref="_outputIndex"/> to 
	/// get the index of the output to switch off.
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
	/// <param name="outIdx">The number of the output to control> array.</param>
	/// <param name="value"><c>1</c> to switch on, <c>0</c> to switch off.</param>
	/// </remarks>
	static void SetDigitalOutput(int outIdx, int value) {
		switch (OutputAxisIdx[outIdx]) {
			case 0:
				switch (OutputPinIdx[outIdx]) {
					case 0:
						Register.Axes_0.Commands.General.DigitalOut1 = value > 0;
						break;
					case 1:
						Register.Axes_0.Commands.General.DigitalOut2 = value > 0;
						break;
					case 2:
						Register.Axes_0.Commands.General.DigitalOut3 = value > 0;
						break;
					case 3:
						Register.Axes_0.Commands.General.DigitalOut4 = value > 0;
						break;
					default:
						// unused 
						break;
				}
				break;
			default:
				// unused 
				break;
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
		var command = Register.Application.TamaControl.IsochronousMainCommand;
		if (Register.Application.TamaControl.IsochronousMainState != command) {
			Register.Application.TamaControl.IsochronousMainState = command;

			_nextTimerIsOff = false;
			_outputIndex = -1;

			if (command == 0) {
				// switched off, tear down current pulse sequence
				for (int i = 0; i < _outputCount; i++) {
					SetDigitalOutput(i, _off);
				}
				_outputCount = 0;
			} else {
				// switched on, evaluate used outputs and calculate number of used outputs 
				var outputs = Register.Application.Variables.Integers[0];
				var j = 0;
				for (var ax = 0; ax < AxesCount; ax++) {
					for (var pin = 0; pin < OutputsPerAxis; pin++) {
						var mask = 1 << (ax * OutputsPerAxis + pin);
						if ((mask & outputs) != 0) {
							OutputAxisIdx[j] = ax;
							OutputPinIdx[j]  = pin;
							j++;
						} else {
							OutputAxisIdx[j] = -1;
							OutputPinIdx[j]  = -1;
						}
					}
				}
				_outputCount = j;

				if (_outputCount > 0) {
					// set initial state 
					_off = Register.Application.Variables.Integers[1];
					for (int i = 0; i < _outputCount; i++) {
						SetDigitalOutput(i, _off);
					}

					// calculate pulse characteristics
					float pulsePeriod = Register.Application.Variables.Floats[0];
					float pulseWidth = Register.Application.Variables.Floats[1];
					if (pulseWidth >= pulsePeriod) {
						// provoke index out of range
						OutputAxisIdx[int.MaxValue] = 0;
						OutputPinIdx[int.MaxValue] = 0;
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

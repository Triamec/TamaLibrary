// Copyright © 2021 Triamec Motion AG

/* Overview
 * --------
 * This pulse pattern generator Tama program is able to output a periodical pulse pattern at TSD Ax0..Ax1 DigOutputs 1..2.
 * The program is configurable via Tama application parameters. This program is based on the implementation 
 * of PulsGenerator.tama.cs 
 * Note: This program is used by the testing framework. 
 * 
 * Inputs:
 * Application.Variables.Floats[0]:   Pulse period [s]
 * Application.Variables.Floats[1]:   Pulse width [s]. If greater than or equal to Pulse period, an IndexOutOfRange error occurs.
 * Application.Variables.Integers[0]: A bit combination identifying the outputs to use. 
 *									  (B0..B1: Ax0:DigOut1..2; B2..B3: Ax1:DigOut1..2)
 * Application.Variables.Integers[1]: Negative logic: Set to 0 if the pulse pattern is to be output unchanged. Set to 1 if pulse pattern must be inverted.
 * IsochronousMainCommand: Set to something different than 0 in order to start.
 * 
 * Outputs:
 * IsochronousMainState:   Set to the value of IsochronousMainCommand
 * OutputBits:			   Output for generated pulses.
 *
 * example of pulse pattern output sequence (output1&2 on Axis0 for 10ms, every 60ms)
 * pulse period = 60ms, pulse width 10ms, pulse pattern 0x3
 * ____           ____           ____
 *     |_________| 03 |_________| 03 |_____ 
 *     |-  60ms     ->|-  60ms     ->| 
 *               |10ms|         |10ms| 
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
/// Tama program for the TSD drive to generate pulse patterns on several outputs.
/// </summary>
[Tama]
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
static class PulsePatternGeneratorTsd {
	#region Constants
	/// <summary>
	/// Number of supported axes.
	/// <para>The value is 2.</para>
	/// </summary>
	const int AxesCount = 2;
	/// <summary>
	/// Number of supported outputs per axis
	/// <para>The value is 2.</para>
	/// </summary>
	const int OutputsPerAxis = 2;
	/// <summary>
	/// Number of supported outputs (1 for all output bits).
	/// <para>The value is 1.</para>
	/// </summary>
	const int OutputCount = 1;

	#endregion Constants

	#region Fields
	/// <summary>
	/// Timer triggering raising and falling edges.
	/// </summary>
	static readonly IsochronousTimer Timer =
		new IsochronousTimer(Register.General.Signals.Internals.CycleTimePathPlanner);

	/// <summary>
	/// used output mask for all outputs.
	/// </summary>
	static readonly int[] OutputMask = new int[OutputCount];

	/// <summary>
	/// Number of used digital outputs.
	/// </summary>
	static int _outputCount;

	/// <summary>
	/// Duration, in seconds, from one pulse to the next.
	/// </summary>
	static float _pulseTime;

	/// <summary>
	/// Duration, in seconds, from a pulse raise to the nearest pulse fall.
	/// </summary>
	static float _pauseTime;

	/// <summary>
	/// Whether the next timer triggers a falling edge.
	/// </summary>
	static bool _nextTimerIsOff;

	/// <summary>
	/// The value of an unset output.
	/// </summary>
	static int _off;

	#endregion Fields

	#region Helpers
	/// <summary>
	/// Sets the specified outputs.
	/// </summary>
	/// <param name="value"><c>1</c> to switch on, <c>0</c> to switch off.</param>
	/// <remarks>
	/// This method assumes exclusive access to the output bits. Bits other than the configured output bits will
	/// be left untouched only if they are static.
	/// </remarks>
	static void SetDigitalOutputs(int value) {
		Register.Axes_0.Commands.General.DigitalOut1 = (value & 0x1) != 0;
		Register.Axes_0.Commands.General.DigitalOut2 = (value & 0x2) != 0;
		Register.Axes_1.Commands.General.DigitalOut1 = (value & 0x4) != 0;
		Register.Axes_1.Commands.General.DigitalOut2 = (value & 0x8) != 0;
	}
	#endregion Helpers

	#region Tasks
	[TamaTask(Task.IsochronousMain)]
	[SuppressMessage("Code Quality", "IDE0051:Remove unused private members", Justification = "Tama entry point")]
	static void IsoMain() {
		// main pulse sequencer
		if ((_outputCount > 0) && Timer.Tick()) {
			if (_nextTimerIsOff) {
				// pause 
				for (int i = 0; i < _outputCount; i++) {
					if (OutputMask[i] != 0) {
						SetDigitalOutputs(_off);
					}
				}
				Timer.Start(_pauseTime);
				_nextTimerIsOff = false;
			} else {
				// pulse 
				for (int i = 0; i < _outputCount; i++) {
					if (OutputMask[i] != 0) {
						SetDigitalOutputs(OutputMask[i] ^ _off);
					}
				}
				_nextTimerIsOff = true;
				Timer.Start(_pulseTime);
			}
		}

		// sync pulse outputs
		var command = Register.Application.TamaControl.IsochronousMainCommand;
		if (Register.Application.TamaControl.IsochronousMainState != command) {
			Register.Application.TamaControl.IsochronousMainState = command;

			_nextTimerIsOff = false;

			if (command == 0) {
				// switched off, tear down current pulse sequence
				for (int i = 0; i < _outputCount; i++) {
					SetDigitalOutputs(_off);
				}
				_outputCount = 0;
			} else {
				// switched on, evaluate used outputs and calculate number of used outputs 
				var outputs = Register.Application.Variables.Integers[0];
				var j = 0;
				OutputMask[j] = 0;
				for (var ax = 0; ax < AxesCount; ax++) {
					for (var pin = 0; pin < OutputsPerAxis; pin++) {
						var mask = 1 << (ax * OutputsPerAxis + pin);
						if ((mask & outputs) != 0) {
							OutputMask[j] |= mask;
						}
					}
				}
				if (OutputMask[j] != 0) {
					j++;
				}
				_outputCount = j;

				if (_outputCount > 0) {
					// get pins logic
					_off = Register.Application.Variables.Integers[1] == 0 ? 0 : 0xffff;

					// set initial state 
					for (int i = 0; i < _outputCount; i++) {
						SetDigitalOutputs(_off);
					}

					// calculate pulse characteristics
					float pulsePeriod = Register.Application.Variables.Floats[0];
					float pulseWidth = Register.Application.Variables.Floats[1];
					if (pulseWidth >= pulsePeriod) {
						// provoke index out of range
						OutputMask[int.MaxValue] = 0;
					}
					_pulseTime = pulseWidth;
					_pauseTime = pulsePeriod - pulseWidth;

					// start first pause 
					Timer.Start(_pauseTime);
					_nextTimerIsOff = false;
				}
			}
		}
	}
	#endregion Tasks
}

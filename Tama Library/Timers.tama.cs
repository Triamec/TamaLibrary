// Copyright © 2008 Triamec Motion AG

using Triamec.Tama.Rlid4;
using Triamec.Tama.Vmid5;
using Triamec.Tam.Samples;

/* Timer Sample
 * ------------
 * 
 * This sample demonstrates the usage of the two library timers, AsynchronousTimer and IsochronousTimer.
 * 
 * As the names suggest, use the isochronous timer in the isochronous main task and the asynchronous timer in the
 * asynchronous main task.
 * 
 * The sample code statically creates a timer for each task and then listens on the main command register for the
 * start signal.
 * When started, the main state is set to 0. After the duration specified in general purpose int or float variables 0 or
 * 1, respectively, the main state is reset to 1.
 * 
 */

[Tama]
public static class TimerSamples {
	static readonly IsochronousTimer Isotimer = new IsochronousTimer(Register.General.Parameters.CycleTimePathPlanner);
	static readonly AsynchronousTimer Asytimer = new AsynchronousTimer();

	[TamaTask(Task.IsochronousMain)]
	public static void Iso() {
		switch (Register.Tama.IsochronousMainCommand) {
			case 1:
				Isotimer.Start(Register.Tama.Variables.GenPurposeIntVar0);
				Register.Tama.IsochronousMainState = 0;
				Register.Tama.IsochronousMainCommand = 3;
				break;

			case 2:
				Isotimer.Start(Register.Tama.Variables.GenPurposeVar0);
				Register.Tama.IsochronousMainState = 0;
				Register.Tama.IsochronousMainCommand = 3;
				break;

			case 3:
				if (Isotimer.Tick()) {
					Register.Tama.IsochronousMainState = 1;
					Register.Tama.IsochronousMainCommand = 0;
				}
				break;
		}
	}

	[TamaTask(Task.AsynchronousMain)]
	public static void Asy() {
		switch (Register.Tama.AsynchronousMainCommand) {
			case 0:
				break;

			case 1:
				Asytimer.Start(Register.Tama.Variables.GenPurposeIntVar1, Register.General.Signals.TriaLinkTimestamp);
				Register.Tama.AsynchronousMainState = 0;
				Register.Tama.AsynchronousMainCommand = 3;
				break;

			case 2:
				Asytimer.Start(Register.Tama.Variables.GenPurposeVar1, Register.General.Signals.TriaLinkTimestamp);
				Register.Tama.AsynchronousMainState = 0;
				Register.Tama.AsynchronousMainCommand = 3;
				break;

			case 3:
				if (Asytimer.Elapsed(Register.General.Signals.TriaLinkTimestamp)) {
					Register.Tama.AsynchronousMainState = 1;
					Register.Tama.AsynchronousMainCommand = 0;
				}
				break;
		}
	}
}

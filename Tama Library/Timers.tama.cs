// Copyright © 2008 Triamec Motion AG

using Triamec.Tama.Rlid19;
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
	static readonly IsochronousTimer Isotimer = new IsochronousTimer(Register.General.Signals.Internals.CycleTimePathPlanner);
	static readonly AsynchronousTimer Asytimer = new AsynchronousTimer();

	[TamaTask(Task.IsochronousMain)]
	public static void Iso() {
		switch (Register.Application.TamaControl.IsochronousMainCommand) {
			case 1:
				Isotimer.Start(Register.Application.Variables.Integers[0]);
				Register.Application.TamaControl.IsochronousMainState = 0;
				Register.Application.TamaControl.IsochronousMainCommand = 3;
				break;

			case 2:
				Isotimer.Start(Register.Application.Variables.Floats[0]);
				Register.Application.TamaControl.IsochronousMainState = 0;
				Register.Application.TamaControl.IsochronousMainCommand = 3;
				break;

			case 3:
				if (Isotimer.Tick()) {
					Register.Application.TamaControl.IsochronousMainState = 1;
					Register.Application.TamaControl.IsochronousMainCommand = 0;
				}
				break;
		}
	}

	[TamaTask(Task.AsynchronousMain)]
	public static void Asy() {
		switch (Register.Application.TamaControl.AsynchronousMainCommand) {
			case 0:
				break;

			case 1:
				Asytimer.Start(Register.Application.Variables.Integers[1], Register.General.Signals.TriaLinkTimestamp);
				Register.Application.TamaControl.AsynchronousMainState = 0;
				Register.Application.TamaControl.AsynchronousMainCommand = 3;
				break;

			case 2:
				Asytimer.Start(Register.Application.Variables.Floats[1], Register.General.Signals.TriaLinkTimestamp);
				Register.Application.TamaControl.AsynchronousMainState = 0;
				Register.Application.TamaControl.AsynchronousMainCommand = 3;
				break;

			case 3:
				if (Asytimer.Elapsed(Register.General.Signals.TriaLinkTimestamp)) {
					Register.Application.TamaControl.AsynchronousMainState = 1;
					Register.Application.TamaControl.AsynchronousMainCommand = 0;
				}
				break;
		}
	}
}

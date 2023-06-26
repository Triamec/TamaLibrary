// Copyright © 2023 Triamec Motion AG

/* Overview
 * --------
 * 
 * 
 * 
 * This file can be used as a basis for custom extensions.
 * 
 * 
 * Shortcomings
 * ------------
 *
 * 
 */

using Triamec.TriaLink;
using Triamec.Tam.Modules;
using Triamec.Tama.Vmid5;
using Triamec.Tama.Rlid19;

#region Fields


#endregion Fields

/// <summary>
/// Tama program for the
/// axis of the module catalog.
/// </summary>
[Tama]
public static class
 Homing19 {


	#region Asynchronous Tama main application
	/// <summary>
	/// The cyclic task.
	/// </summary>
	[TamaTask(Task.AsynchronousMain)]
	public static void Homer() {

		#region State machine
		switch (Register.Axes_0.Signals.Homing.State) {

			#region state idle -> wait for command to execute
			case HomingState.Idle:

                // keep TamaState idle if HomingState is idle
                Register.Application.TamaControl.AsynchronousMainState = TamaState.Idle;

                switch (Register.Axes_0.Commands.Homing.Command) {

					case HomingCommand.Start:

						// automatic transition to TamaProgramRequest

						break;
				}
				break;
			#endregion state idle -> wait for command to execute

			case HomingState.TamaProgramRequested:

                // reset homing command for handshake and transition to TamaProgramRunning
                Register.Axes_0.Commands.Homing.Command = HomingCommand.None;
                // make sure the TamaState is idle before letting it run
                Register.Application.TamaControl.AsynchronousMainState = TamaState.Idle;
                break;

			case HomingState.TamaProgramRunning:

				switch(Register.Application.TamaControl.AsynchronousMainState) {

                    case TamaState.Idle:

						// start preprocessing
                        Register.Application.TamaControl.AsynchronousMainState = TamaState.Preprocessing;
                        break;


					case TamaState.Preprocessing:

						// execute preprocessing
						//...
						Register.Application.Variables.Booleans[0] = true;

                        // start actual homing procedure
                        Register.Application.TamaControl.AsynchronousMainState = TamaState.Homing;
                        break;

					case TamaState.Homing:

						// start Homing Standard routine or self written Tama routine here
						Register.Axes_0.Commands.Homing.Command = HomingCommand.Start;

                        // start postprocessing after return from homing
                        Register.Application.TamaControl.AsynchronousMainState = TamaState.Postprocessing;
                        break;

					case TamaState.Postprocessing:

                        // execute postprocessing
                        //...
                        Register.Application.Variables.Booleans[1] = true;

                        // set homing to done
                        Register.Axes_0.Commands.Homing.Command = HomingCommand.SetHomingDone;
                        Register.Application.TamaControl.AsynchronousMainState = TamaState.Done;
                        break;

                    case TamaState.Done:

                        // jump to preprocessing to execute homing
                        Register.Application.TamaControl.AsynchronousMainState = TamaState.Preprocessing;
                        break;
                }
			    break;

            case HomingState.HomingDone:

                switch (Register.Axes_0.Commands.Homing.Command) { 

                    case HomingCommand.Start: 

                        // invalidate to go back to HomingState idle and the start
                        Register.Axes_0.Commands.Homing.Command = HomingCommand.Invalidate;
                        Register.Axes_0.Commands.Homing.Command = HomingCommand.Start;
                        break;

                }
            

                switch (Register.Application.TamaControl.AsynchronousMainState) {

                    case TamaState.Done:
                        //wait for commands
                        break;
                }
                break;


        }

        #endregion State machine
    }
    #endregion Asynchronous Tama main application
}
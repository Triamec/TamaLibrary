// Copyright © 2023 Triamec Motion AG

/* Overview
 * --------
 * This file shows an implementation of a homing procedure with a TamaProgram.
 * The TamaProgram is implemented according to the state machine described in AN141 
 * (https://www.triamec.com/en/documents.html?file=files/medien/documents/AppNotes/AN141_HomingProceduresAndSetup_EP003.pdf&cid=3411)
 * Within this TamaProgram, the standard homing routine implemented on the firmware is called. You can replace it with your own homing routine if necessary.
 * In order to verify that the homing has been truly executed by the TamaProgram, the Booleans in Application/Variables/Booleans/ are toggled.
 * This file can be used as a basis for custom extensions.
 * 
 * How to run this program:
 * 1. Build it
 * 2. Open TAM System explorer
 * 3. In the topology, navigate to your drives 'Tama Manager'
 * 4. Right-click the 'Tama Manager' and Select 'Load Tama assembly...'
 * 5. Navigate to and load the *.tama file (usually in the /bin of the current project)
 * 6. In the topology, right-click on the 'Tama Manager' and select 'Enable asynchronous Tama VM'
 * 7. In the topology, change Axes[0]/Parameters/Homing/Method to 'TamaProgram'
 * 8. Start the homing (Axis Module's 'Home' Button, Axes[0]/Commands/Homing/Command -> 'Start', API etc.)
 * 9. Check the state of Application/Variables/Booleans/ to make sure homing is executed via TamaProgram
 * 
 * Shortcomings
 * ------------
 * - This program only shows the skeleton for your custom homing state machine but no custom homing routine
 * 
 */

//using Triamec.TriaLink;
using Triamec.Tam.Modules;
using Triamec.Tama.Vmid5;
using Triamec.Tama.Rlid19;

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

		#region Homing state machine
		switch (Register.Axes_0.Signals.Homing.State) {

			#region state Idle -> wait for homing command
			case HomingState.Idle:

                // keep TamaState idle if HomingState is idle
                Register.Application.TamaControl.AsynchronousMainState = TamaState.Idle;

                switch (Register.Axes_0.Commands.Homing.Command) {

					case HomingCommand.Start:

						// automatic transition to TamaProgramRequest

						break;
				}
				break;
            #endregion state idle -> wait for homing command

            #region state TamaProgramRequested -> handshake to verify homing program, switch to TamaProgramRunning
            case HomingState.TamaProgramRequested:

                // reset homing command for handshake and transition to TamaProgramRunning
                Register.Axes_0.Commands.Homing.Command = HomingCommand.None;
                break;
            #endregion state TamaProgramRequested -> handshake to verify homing program, switch to TamaProgramRunning

            #region Tama state machine
            case HomingState.TamaProgramRunning:

				switch(Register.Application.TamaControl.AsynchronousMainState) {

                    case TamaState.Idle:

						// start preprocessing
                        Register.Application.TamaControl.AsynchronousMainState = TamaState.Preprocessing;
                        break;


					case TamaState.Preprocessing:

						// execute preprocessing
						//...

                        // indicate preprocessing start with booleans
						Register.Application.Variables.Booleans[0] = true;
                        Register.Application.Variables.Booleans[1] = false;


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

                        // indicate postprocessing start with boolean
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
            #endregion Tama state machine

            #region state HomingDone -> wait for new homing command
            case HomingState.HomingDone:

                // indicate end of TamaHoming with boolean
                Register.Application.Variables.Booleans[0] = false;

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
            #endregion state HomingDone -> wait for new homing command
        }

        #endregion Homing state machine
    }
    #endregion Asynchronous Tama main application
}
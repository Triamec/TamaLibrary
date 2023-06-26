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
		switch (Register.Application.TamaControl.AsynchronousMainState) {

			#region state idle -> wait for command to execute
			case TamaState.Idle:
				switch (Register.Application.TamaControl.AsynchronousMainCommand) {

					case TamaCommand.StartHoming:
						// homing with search of index requested

						// get the search dynamic parameters and saves the actual DRF value 
						// --------------------------------------------------------------------
						//GetSearchDynamicParameters();

						// switch to state 'CheckForHomingAction'
						Register.Application.TamaControl.AsynchronousMainState = TamaState.CheckForHomingAction;

						// reset the tama command 
						Register.Application.TamaControl.AsynchronousMainCommand = TamaCommand.NoCommand;

						break;
				}
				break;
				#endregion state idle -> wait for command to execute
		}
        #endregion State machine
    }
    #endregion Asynchronous Tama main application
}
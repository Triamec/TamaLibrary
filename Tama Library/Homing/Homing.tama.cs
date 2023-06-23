// Copyright © 2012 Triamec Motion AG

/* Overview
 * --------
 * 
 * This is the source code for the Tama homing programs shipped with the TAM Software (in the RC subdirectory of the 
 * installation).
 * The axis module offers homing support, when the "Omit homing" parameter is set to false and an appropriate homing
 * Tama program is loaded. After enabling the axis, the homing button will trigger the StartHoming command, and the
 * axis GUI will reflect start and end of the homing sequence. It's possible to override the default search speed, 
 * the homing direction and the index logic. 
 * - The parameter Register.Axes.Axes[0].Parameters.Environment.ReferencePosition
 *   (or Register.Application.Parameters.Doubles[0], for more recent layouts) will be set at the found index position.
 * - It is possible to override the 'move speed to use during the homing moves' within Register.Tama.Variables.GenPurposeVar0.
 *   - The default value is 0.1 m/s or rad/s 
 * - It is possible to override the 'move homing direction to use during the homing moves' within Register.Tama.Variables.GenPurposeIntVar0.
 *   - GenPurposeIntVar0 = 0 -> homeDirectionPositive = false -> searches backwards (default)
 *   - GenPurposeIntVar0 > 0 -> homeDirectionPositive = true  -> searches forwards
 * - It is possible to override the 'index Logic definition' within Register.Tama.Variables.GenPurposeIntVar1.
 *   - GenPurposeIntVar1 = 0 -> indexLogicNegative = false -> positive logic (default)
 *   - GenPurposeIntVar1 > 0 -> indexLogicNegative = true  -> negative logic
 * 
 * Further more, during homing, the Stop button will trigger the Stop command.
 * 
 * This file can be used as a basis for custom extensions.
 * 
 * 
 * Support for different register layouts
 * --------------------------------------
 * 
 * In order to support different register layouts with the same lines of code, conditional compilation symbols have been
 * defined. Instead of the usual Debug/Release project and solution configurations, the project defines configurations
 * referring to the different supported register layouts. Using solution batch build, all homing programs may be
 * compiled in one pass.
 * 
 * 
 * Shortcomings
 * ------------
 *
 * Some register layouts (5, 6 and 16), support two axes, while the presented homing program always operates on the
 * first axis. Using an #if conditional directive in every call to an axis register would introduce too much clutter in
 * most cases. Therefore, in order to support the second axis, two versions of homing programs need to be maintained.
 * In order to support both axes simultaneously, the program needed to be refactored accordingly, i.e. additional
 * states and commands.
 * 
 * Drives with register layout 19 and current firmware have homing functionality built-in. However, it doesn't currently
 * integrate with the TAM Software axis module.
 * 
 */

using Triamec.TriaLink;
using Triamec.Tam.Modules;
using Triamec.Tama.Vmid5;
#if RLID4
using Triamec.Tama.Rlid4;
#elif RLID5
using Triamec.Tama.Rlid5;
#elif RLID6
using Triamec.Tama.Rlid6;
#elif RLID16
using Triamec.Tama.Rlid16;
#elif RLID17
using Triamec.Tama.Rlid17;
#elif RLID19
using Triamec.Tama.Rlid19;
#endif

/// <summary>
/// Tama program for the
/// axis of the module catalog.
/// </summary>
[Tama]
public static class
#if RLID4
 Homing04
#elif RLID5
 Homing05
#elif RLID6
 Homing06
#elif RLID16
 Homing16
#elif RLID17
 Homing17
#elif RLID19
 Homing19
#endif
 {
	#region Fields
	/// <summary>
	/// The next state to set after move is done.
	/// </summary>
	static int _stateAfterMoveDone;

	/// <summary>
	/// The original dynamic reduction factor to set after homing and touchdown search.
	/// </summary>
	static float _originalDrf;

	/// <summary>
	/// The move speed to use during the homing moves. [m/s or red/s]
	/// It is possible to override this parameter within Register.Tama.Variables.GenPurposeVar0.
	/// The default value is 0.1 m/s or red/s 
	/// </summary>
	static float _searchSpeed;
	const float SearchSpeedDefault = 0.1f;

	/// <summary>
	/// The homing move direction to use during the homing moves. [-]
	/// It is possible to override this parameter within Register.Tama.Variables.GenPurposeIntVar0.
	/// The default value is false 
	/// </summary>
	static bool _homeDirectionPositive;

	/// <summary>
	/// The index Logic definition. [-]
	/// indexLogicNegative = false -> positive logic (default)
	/// indexLogicNegative = true  -> negative logic
	/// It is possible to override this parameter within Register.Tama.Variables.GenPurposeIntVar1.
	/// </summary>
	static bool _indexLogicNegative;


	/// <summary>
	/// The detected index position [rad]
	/// </summary>
	static 
		#if DOUBLE
		double
		#else
		float 
		#endif
		_indexPosition;

#if !DOUBLE
	static int _indexPositionExtension;
#endif

	#endregion Fields

	#region Asynchronous Tama main application
	/// <summary>
	/// The cyclic task.
	/// </summary>
	[TamaTask(Task.AsynchronousMain)]
	public static void Homer() {
		#region State machine
		switch (Register.
			#if RLID17 || RLID19
			Application.TamaControl
			#else
			Tama
			#endif
			.AsynchronousMainState) {

			#region state idle -> wait for command to execute
			case TamaState.Idle:
				switch (Register.
					#if RLID17 || RLID19
					Application.TamaControl
					#else
					Tama
					#endif
					.AsynchronousMainCommand) {

					case TamaCommand.StartHoming:
						// homing with search of index requested

						// get the search dynamic parameters and saves the actual DRF value 
						// --------------------------------------------------------------------
						GetSearchDynamicParameters();

						// switch to state 'CheckForHomingAction'
						Register.
							#if RLID17 || RLID19
							Application.TamaControl
							#else
							Tama
							#endif
							.AsynchronousMainState = TamaState.CheckForHomingAction;

						// reset the tama command 
						Register.
							#if RLID17 || RLID19
							Application.TamaControl
							#else
							Tama
							#endif
							.AsynchronousMainCommand = TamaCommand.NoCommand;

						break;
				}
				break;
			#endregion state idle -> wait for command to execute


			#region state CheckForHomingAction -> checks for needed homing action
			case TamaState.CheckForHomingAction:
				// check for stop command
				if (!StopRequestedOrError()) {
					// homing with search of index requested
					// check if axis is at index initial
					if (IsAxisAtIndex()) {
						// axis is at index initial 
						// -> move away from index within velocity move in opposite home direction 
						StartSearchMove(false);

						// switch to WaitIndexCleared state
						Register.
							#if RLID17 || RLID19
							Application.TamaControl
							#else
							Tama
							#endif
							.AsynchronousMainState = TamaState.WaitIndexCleared;
					} else {
						// axis is not at index
						// enable index latching
						Register.Axes_0.Commands
							#if RLID19
							.PositionController.PositionLatchStandard.Enable
							#else
							.General.EncoderIndexLatchEnable
							#endif
							= true;

						// -> start searching index within velocity move in home direction
						StartSearchMove(true);

						// switch to SearchLatch state
						Register.
							#if RLID17 || RLID19
							Application.TamaControl
							#else
							Tama
							#endif
							.AsynchronousMainState = TamaState.SearchIndex;
					}
				}
				break;
			#endregion state CheckForHomingAction -> checks for needed homing action


			#region state wait index cleared state -> move till index is cleared and watch stop command
			case TamaState.WaitIndexCleared:
				// check for stop command
				if (!StopRequestedOrError()) {
					// check if index is cleared	
					if (!IsAxisAtIndex()) {
						// index cleared -> stop axis fast (stops axis near index position)
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.Stop;

						// switch to state 'wait for move done' and 
						// set state after move done to CheckForHomingAction
						Register.
							#if RLID17 || RLID19
							Application.TamaControl
							#else
							Tama
							#endif
							.AsynchronousMainState = TamaState.WaitMoveDone;

						_stateAfterMoveDone = TamaState.CheckForHomingAction;

					} else {
						// latch still not cleared -> stay in wait state 
					}
				}
				break;
			#endregion state wait index cleared state -> move till index is cleared and watch stop command


			#region state search index -> search index and watch stop command
			case TamaState.SearchIndex:
				// check for stop command
				if (!StopRequestedOrError()) {
					// watch index latch and stop  
					// --------------------------------------------------------------------
					if (IndexLatched()) {
						// at index -> safe index position
						_indexPosition = Register.Axes_0.Signals.PositionController
							#if RLID19
							.PositionLatchStandard.Position
							#else
							.LatchedPosition
							#endif
							#if !DOUBLE
							.Float32
							#endif
							;

						#if !DOUBLE
						_indexPositionExtension = Register.Axes_0.Signals.PositionController.LatchedPosition.Extension;
						#endif

						// drive axis fast to position at index event (stops axis at index position)
						Register.Axes_0.Commands.PathPlanner.Xnew
							#if !DOUBLE
							.Float32
							#endif
							= _indexPosition;
						
						#if !DOUBLE
						Register.Axes_0.Commands.PathPlanner.Xnew.Extension = _indexPositionExtension;
						#endif

						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;

						// switch to state 'wait for move done' and 
						// set state after move done to setPosition
						// --------------------------------------------------------------------
						Register.
							#if RLID17 || RLID19
							Application.TamaControl
							#else
							Tama
							#endif
							.AsynchronousMainState = TamaState.WaitMoveDone;
						
						_stateAfterMoveDone = TamaState.SetPosition;
					}
				}
				break;
			#endregion state search index -> start search move, search index and watch stop command


			#region state SetPosition -> check for consistency, set new PathPlanner position and controller position
			case TamaState.SetPosition:
				// set new axis position to provided reference position value.
				// rem: referencePosition is the position of the index 
				Register.Axes_0.Commands.PathPlanner.Xnew
					#if !DOUBLE
					.Float32
					#endif							
					= Register
					#if RLID19
					.Application.Parameters.Doubles[0]
					#else
					.Axes_0.Parameters.Environment.ReferencePosition
					#endif
				;


				#if !DOUBLE
				Register.Axes_0.Commands.PathPlanner.Xnew.Extension = 0;
				#endif

				// perform setPosition command 	
				Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.SetPosition;

				// set state to waitPosSet
				Register.
					#if RLID17 || RLID19
					Application.TamaControl
					#else
					Tama
					#endif
					.AsynchronousMainState = TamaState.WaitPositionSet;
				break;
			#endregion state SetPosition -> set new PathPlanner position and controller position


			#region state WaitPositionSet -> wait until new position is set
			case TamaState.WaitPositionSet:
				// check for origin translation is done 
				// --------------------------------------------------------------------
				if (Register.Axes_0.Signals.PathPlanner.Done) {
					// move axis to position 0.0
					Register.Axes_0.Commands.PathPlanner.Xnew
						#if !DOUBLE
						.Float32
						#endif
						= 0.0f;
					#if !DOUBLE
					Register.Axes_0.Commands.PathPlanner.Xnew.Extension = 0;
					#endif

					Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;

					// switch to state 'wait for move done' and 
					// set state after move done to idle
					Register.
						#if RLID17 || RLID19
						Application.TamaControl
						#else
						Tama
						#endif
						.AsynchronousMainState = TamaState.WaitMoveDone;
					
					_stateAfterMoveDone = TamaState.Idle;
				}
				break;
			#endregion state WaitPositionSet -> wait until new position is set


			#region state waitMoveDone -> wait for move is done
			case TamaState.WaitMoveDone:
				// check for done 
				// --------------------------------------------------------------------
				if (Register.Axes_0.Signals.PathPlanner.Done) {
					if (_stateAfterMoveDone == TamaState.Idle) {
						// restore the DRF and velocity settings
						Register.Axes_0.Parameters.PathPlanner.DynamicReductionFactor = _originalDrf;
						Register.Axes_0.Commands.PathPlanner.CommitParameter = true;
					}
					// switch to requested state after move done 
					Register.
						#if RLID17 || RLID19
						Application.TamaControl
						#else
						Tama
						#endif
						.AsynchronousMainState = (int)_stateAfterMoveDone;
				} else {
					if (!StopRequestedOrError()) { }
				}
				break;
			#endregion state waitMoveDone -> wait for move is done


		}
		#endregion State machine
	}
	#endregion Asynchronous Tama main application


	#region local methods of asynchronous Tama main application

	/// <summary>
	/// checks for stop request or axis error
	/// - performs stop actions and sets the state to WaitMoveDone and nextState to idle
	///   returns true if stop is requested; false otherwise
	/// - checks for error conditions.
	///   - reset orig DRF in case of error, sets nextState to motionError and returns true;
	///   - returns false in no error cases
	/// </summary>
	static bool StopRequestedOrError() {
		if (Register.
				#if RLID17 || RLID19
				Application.TamaControl
				#else
				Tama
				#endif
				.AsynchronousMainCommand == TamaCommand.Stop) {

			// stop is requested, reset the tama command 
			Register.
				#if RLID17 || RLID19
				Application.TamaControl
				#else
				Tama
				#endif
				.AsynchronousMainCommand = TamaCommand.NoCommand;

			// fast stop axis
			Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.Stop;

			// switch to state 'wait for move done' and 
			// set state after move done to idle
			Register.
				#if RLID17 || RLID19
				Application.TamaControl
				#else
				Tama
				#endif
				.AsynchronousMainState = TamaState.WaitMoveDone;

			_stateAfterMoveDone = TamaState.Idle;
			return true;
		}
		// check for axis error
		if (Register.Axes_0.Signals.General.AxisError != AxisErrorIdentification.None) {
			// any axis error occurred
			// restore the DRF and velocity settings
			Register.Axes_0.Parameters.PathPlanner.DynamicReductionFactor = _originalDrf;
			Register.Axes_0.Commands.PathPlanner.CommitParameter = true;
			// immediately set state to error 
			Register.
				#if RLID17 || RLID19
				Application.TamaControl
				#else
				Tama
				#endif
				.AsynchronousMainState = TamaState.Idle;

			// reset the tama command 
			Register.
				#if RLID17 || RLID19
				Application.TamaControl
				#else
				Tama
				#endif
				.AsynchronousMainCommand = TamaCommand.NoCommand;
			
			return true;
		}
		return false;
	}


	/// <summary>
	/// GetSearchDynamicParameters
	/// gets the search speed parameter and saves the actual DRF value. 
	/// </summary>
	static void GetSearchDynamicParameters() {
		// get override search speed value from register, if value is > 0.0 
		// --------------------------------------------------------------------
		if (Register.
			#if RLID17 || RLID19
			Application.Variables.Floats[0]
			#else
			Tama.Variables.GenPurposeVar0
			#endif
			> 0.0f) {

			_searchSpeed = Register.
				#if RLID17 || RLID19
				Application.Variables.Floats[0]
				#else
				Tama.Variables.GenPurposeVar0
				#endif
				;
		} else {
			// take the default value
			_searchSpeed = SearchSpeedDefault;
		}
		// show actual search speed in register
		Register.
			#if RLID17 || RLID19
			Application.Variables.Floats[0]
			#else
			Tama.Variables.GenPurposeVar0
			#endif
			= _searchSpeed;

		// get override homing direction value from register, if value is > 0 
		// --------------------------------------------------------------------
		if (Register.
			#if RLID17 || RLID19
			Application.Variables.Integers[0]
			#else
			Tama.Variables.GenPurposeIntVar0
			#endif
			> 0) {
			
			_homeDirectionPositive = Register.
				#if RLID17 || RLID19
				Application.Variables.Integers[0]
				#else
				Tama.Variables.GenPurposeIntVar0
				#endif
				> 0;

		} else {
			// take the default value
			_homeDirectionPositive = false;
		}

		// show actual homeDirection in register
		Register.
			#if RLID17 || RLID19
			Application.Variables.Integers[0]
			#else
			Tama.Variables.GenPurposeIntVar0
			#endif
			= _homeDirectionPositive ? 1 : 0;

		// get override index logic value from register, if value is > 0 
		// --------------------------------------------------------------------
		if (Register.
			#if RLID17 || RLID19
			Application.Variables.Integers[1]
			#else
			Tama.Variables.GenPurposeIntVar1
			#endif
			> 0) {

			_indexLogicNegative = Register.
				#if RLID17 || RLID19
				Application.Variables.Integers[1]
				#else
				Tama.Variables.GenPurposeIntVar1
				#endif
			 > 0;

		} else {
			// take the default value
			_indexLogicNegative = false;
		}
		// show actual index logic in register
		Register.
			#if RLID17 || RLID19
			Application.Variables.Integers[1]
			#else
			Tama.Variables.GenPurposeIntVar1
			#endif
			= _indexLogicNegative ? 1 : 0;


		// disable index latching (default state)
		// --------------------------------------------------------------------
		Register.Axes_0.Commands
			#if RLID19
			.PositionController.PositionLatchStandard.Enable
			#else
			.General.EncoderIndexLatchEnable
			#endif
			= false;

		// safe the orig DRF
		// --------------------------------------------------------------------
		_originalDrf = Register.Axes_0.Parameters.PathPlanner.DynamicReductionFactor;
		// set DRF for the search moves 
		Register.Axes_0.Parameters.PathPlanner.DynamicReductionFactor = 1.0f;
		Register.Axes_0.Commands.PathPlanner.CommitParameter = true;
	}


	/// <summary>
	/// startSearchMove
	/// sets the searchSpeed depending on parameters moveInHomeDirection and HomeDirectionPositive.
	/// starts the velocity move afterwards. 
	/// <param name="moveInHomeDirection">moves in home direction if value is true.</param>
	/// </summary>
	static void StartSearchMove(bool moveInHomeDirection) {
		// start searching within velocity move with defined search speed
		if (_homeDirectionPositive) {
			Register.Axes_0.Commands.PathPlanner.Vnew = moveInHomeDirection ? _searchSpeed : -_searchSpeed;
		} else {
			Register.Axes_0.Commands.PathPlanner.Vnew = moveInHomeDirection ? -_searchSpeed : _searchSpeed;
		}
		Register.Axes_0.Commands.PathPlanner.Direction = PathPlannerDirection.Current;
		Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveVelocity;
	}


	/// <summary>
	/// isAxisAtIndex
	/// Reads the EncoderIndex register and returns true if axis is at index; returns false otherwise
	/// </summary>
	static bool IsAxisAtIndex() {
		// get actual encoderIndex state
		if (_indexLogicNegative) {
			//(bit reset == at index)
			return !Register.Axes_0.Signals
				#if !RLID19
					.General
				#else
					.PositionController.PositionLatchStandard
				#endif
					.EncoderIndex;
		} else {
			//(bit set == at index)
			return Register.Axes_0.Signals
				#if !RLID19
					.General
				#else
					.PositionController.PositionLatchStandard
				#endif
					.EncoderIndex;
		}
	}

	/// <summary>
	/// Reads the EncoderIndex latch register and returns true if index latch has triggered; returns false otherwise
	/// </summary>
	static bool IndexLatched() =>
		
		// get actual encoderIndex latch state (bit set == latched)
		Register.Axes_0.Signals
			#if !RLID19
				.General.EncoderIndexLatch;
			#else
				.PositionController.PositionLatchStandard.State == PositionLatchState.Found;
			#endif


	#endregion local methods of asynchronous Tama main application

}

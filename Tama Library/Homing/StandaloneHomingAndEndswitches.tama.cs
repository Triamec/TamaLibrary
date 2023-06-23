// Copyright © 2007 Triamec Motion AG

/* Overview
 * --------
 * 
 * input signals:
 * 
 * din(0) = enable
 * din(1) = home
 * din(2) = move
 * 
 * din(3) = position limit in negative direction reached
 * din(4) = position limit in positive direction reached
 * 
 * din(5) = side
 * 
 * output signals:
 * 
 * dout(1) = 0        -> no error pending
 * dout(1) = 1        -> error pending
 * 
 * dout(2) = 0        -> no action
 * dout(2) = 1        -> standstill 
 * dout(2) = blinking -> moving 
 * 
 * The Triamec I/O Helper is helpful for testing the program.
 * 
 * Note that drives with register layout 19 and current firmware have homing functionality built-in.
 */

using Triamec.Tam.Samples;
using Triamec.Tama.Rlid19;
using Triamec.Tama.Vmid5;
using Triamec.TriaLink;

[Tama]
static class StandaloneHomingAndEndswitches {
	#region Constants
	static class State {
		public const int Disabled = 0;
		public const int Enabled = 1;
		public const int Idle = 2;
		public const int MovingToMiddlePositive = 3;
		public const int MovingToMiddleNegative = 4;
		public const int MovingToReferenceStart = 5;
		public const int MovingToReference = 6;
		public const int MovingToHome = 7;
		public const int MovingToPosition1 = 8;
		public const int MovingToPosition2 = 9;
	}


	// homing velocities [m/s] or [rad/s]
	const float VelocityToMiddle = 10f;
	const float VelocityToReference = 1f;

	// homing specific positions
	const float RelativeReferenceStartPosition = 0.001f;
	const float DesiredReferencePosition = 0;
	const float HomePosition = 0;
	const float Position1 = -3;
	const float Position2 = 3;

	// blinking duration, in seconds
	const float Duration = 0.5f;

	#endregion Constants

	#region Fields

	// digital signals
	static bool _commitError = true;
	static int _state;

	#endregion Fields

	#region Constructor
	/// <summary>
	/// Initializes the <see cref="StandaloneHomingAndEndswitches"/> class.
	/// </summary>
	static StandaloneHomingAndEndswitches() {
		BlinkTimer.Start(Duration, Register.General.Signals.TriaLinkTimestamp);
	}
	#endregion Constructor

	#region user functions

	static readonly AsynchronousTimer BlinkTimer = new AsynchronousTimer();
	static bool _blinkFlag;

	static bool Blink() {
		if (BlinkTimer.Elapsed(Register.General.Signals.TriaLinkTimestamp)) {
			_blinkFlag = !_blinkFlag;
			BlinkTimer.Start(Duration, Register.General.Signals.TriaLinkTimestamp);
		}
		return _blinkFlag;
	}

	static bool GetDigitalIn(int channel) => ((Register.Axes_0.Signals.General.DigitalInputBits & (1u << channel)) != 0);

	static bool EnableDrive(bool enable) {
		bool done = false;

		if (enable) {
			if (Register.General.Signals.DriveState != DeviceState.Operational) {
				// switch the drive on
				Register.General.Commands.Internals.Event = DeviceEvent.SwitchOn;
			} else {
				// startup system
				switch (Register.Axes_0.Signals.General.AxisState) {

					case AxisState.Disabled:
						if (_commitError) {
							// proforma clear axis error
							Register.Axes_0.Commands.General.Event = AxisEvent.ResetError;
							_commitError = false;
						} else {
							// enable axis
							Register.Axes_0.Commands.General.Event = AxisEvent.EnableAxis;
						}
						break;

					case AxisState.NotReady:
					case AxisState.Enabling:
					case AxisState.ErrorStopping:
						break;

					default:
						// drive is enabled
						done = true;
						break;
				}
			}
		} else {
			// force an axis error reset
			// when enabling the drive next time
			_commitError = true;

			if (Register.General.Signals.DriveState == DeviceState.FaultPending) {

				// commit pending drive error
				Register.General.Commands.Internals.Event = DeviceEvent.ResetFault;
			} else {
				// shutdown drive
				switch (Register.Axes_0.Signals.General.AxisState) {

					case AxisState.ContinuousMotion:
						// stop axis
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.Stop;
						break;

					case AxisState.Stopping:
						// complete stopping (nothing to do)
						break;

					case AxisState.Standstill:
						// standstill reached, disable axis
						Register.Axes_0.Commands.General.Event = AxisEvent.DisableAxis;
						break;

					case AxisState.Disabled:
						// switch off drive
						Register.General.Commands.Internals.Event = DeviceEvent.SwitchOff;
						break;

					case AxisState.NotReady:

						switch (Register.General.Signals.DriveState) {
							case DeviceState.Operational:
								// erroneous state
								Register.General.Commands.Internals.Event = DeviceEvent.SwitchOff;
								break;

							default:
								break;

						}
						break;

					default:
						// all other states are directly disabled
						Register.Axes_0.Commands.General.Event = AxisEvent.DisableAxis;
						break;

				}
			}
		}

		return done;

	}

	#endregion user functions

	#region Standalone application
	/// <summary>
	/// The standalone application.
	/// </summary>
	[TamaTask(Task.AsynchronousMain)]
	public static void StandAloneApplication() {

		// digital inputs
		bool enable = GetDigitalIn(0);
		bool home = GetDigitalIn(1);
		bool move = GetDigitalIn(2);
		bool side = GetDigitalIn(5);

		// error handling
		bool axisError = Register.Axes_0.Signals.General.AxisError != AxisErrorIdentification.None;
		bool driveError = Register.General.Signals.DriveError != DeviceErrorIdentification.None;

		// LEDs
		Register.Axes_0.Commands.General.DigitalOut1 = (axisError && !_commitError) || driveError;
		Register.Axes_0.Commands.General.DigitalOut2 = (_state == State.Idle) || ((_state > State.Idle) && Blink());


		if (EnableDrive(enable)) {

			// Drive is enabled
			switch (_state) {
				case State.Disabled:
					_state = State.Enabled;
					break;

				case State.Enabled:
					if (home) {
						Register.Axes_0.Commands.PathPlanner.Direction = PathPlannerDirection.Current;
						Register.Axes_0.Commands.PositionController.PositionLatchStandard.Enable = false;
						if (side) {
							Register.Axes_0.Commands.PathPlanner.Vnew = VelocityToMiddle;
							_state = State.MovingToMiddlePositive;
						} else {
							Register.Axes_0.Commands.PathPlanner.Vnew = -VelocityToMiddle;
							_state = State.MovingToMiddleNegative;
						}
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveVelocity;
					}
					break;

				case State.MovingToMiddleNegative:
					if (side) {

						// middle found -> move to the start of the reference position
						Register.Axes_0.Commands.PathPlanner.Xnew = Register.Axes_0.Signals.PathPlanner.Position +
							RelativeReferenceStartPosition;

						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute_Vel;
						_state = State.MovingToReferenceStart;
					}
					break;

				case State.MovingToMiddlePositive:
					if (!side) {

						// middle found -> move to the start of the reference position
						Register.Axes_0.Commands.PathPlanner.Xnew = Register.Axes_0.Signals.PathPlanner.Position +
							RelativeReferenceStartPosition;

						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute_Vel;
						_state = State.MovingToReferenceStart;
					}
					break;

				case State.MovingToReferenceStart:
					if (Register.Axes_0.Signals.PathPlanner.Done) {

						// start search of the encoder reference
						Register.Axes_0.Commands.PathPlanner.Vnew = VelocityToReference;
						Register.Axes_0.Commands.PositionController.PositionLatchStandard.Enable = true;
						Register.Axes_0.Commands.PathPlanner.Direction = PathPlannerDirection.Negative;
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveVelocity;
						_state = State.MovingToReference;
					}
					break;

				case State.MovingToReference:

					// the DSP accepted the latch command
					if (Register.Axes_0.Commands.PositionController.PositionLatchStandard.Enable) {

						// reference found -> move to home position
						Register.Axes_0.Commands.PathPlanner.Xnew = HomePosition +
							Register.Axes_0.Signals.PositionController.PositionLatchStandard.Position -
							DesiredReferencePosition;

						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
						_state = State.MovingToHome;
					}
					break;

				case State.MovingToHome:
					if (Register.Axes_0.Signals.PathPlanner.Done) {

						// home reached -> set the new position reference
						Register.Axes_0.Commands.PathPlanner.Xnew = HomePosition;
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.SetPosition;
						_state = State.Idle;
					}
					break;

				case State.Idle:
					if (move) {

						// go to position 1
						_state = State.MovingToPosition1;
						Register.Axes_0.Commands.PathPlanner.Xnew = Position1;
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
					}
					break;

				case State.MovingToPosition1:
					if (!move) {

						// go to home position
						_state = State.Idle;
						Register.Axes_0.Commands.PathPlanner.Xnew = HomePosition;
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
					} else {
						if (Register.Axes_0.Signals.PathPlanner.Done) {

							// go to position 2
							_state = State.MovingToPosition2;
							Register.Axes_0.Commands.PathPlanner.Xnew = Position2;
							Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
						}
					}
					break;

				case State.MovingToPosition2:
					if (!move) {

						// go to home position
						_state = State.Idle;
						Register.Axes_0.Commands.PathPlanner.Xnew = HomePosition;
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
					} else {
						if (Register.Axes_0.Signals.PathPlanner.Done) {

							// go to position 1
							_state = State.MovingToPosition1;
							Register.Axes_0.Commands.PathPlanner.Xnew = Position1;
							Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.MoveAbsolute;
						}
					}
					break;
			}
		} else {
			// Drive is not ready
			_state = State.Disabled;
		}

		Register.Application.TamaControl.AsynchronousMainState = _state;
	}
	#endregion Standalone application

	#region Endpoint detection
	/// <summary>
	/// Stops the drive depending on two end switches.
	/// </summary>
	[TamaTask(Task.IsochronousMain)]
	static void DetectEndpoints() {
		bool lowLimit = GetDigitalIn(3);
		bool highLimit = GetDigitalIn(4);

		AxisState axisState = Register.Axes_0.Signals.General.AxisState;

		// Let the path planner execute a commanded error stop
		if (axisState == AxisState.ErrorStopping) return;

		if (axisState > AxisState.Standstill) {
			// When moving...

			// Issue a full dynamics stop when lower limit is reached and moving in negative direction, or
			// when higher limit is reached and moving in positive direction.
			if ((lowLimit && (Register.Axes_0.Signals.PathPlanner.Velocity < 0f)) ||
				(highLimit && (Register.Axes_0.Signals.PathPlanner.Velocity > 0f))) {

				Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.EmergencyStop;
			}
		} else {
			// When stand still...

			switch (Register.Axes_0.Commands.PathPlanner.Command) {
				case PathPlannerCommand.MoveAbsolute:
				case PathPlannerCommand.MoveAbsolute_Vel:
				case PathPlannerCommand.MoveAbsolute_VelAcc:

					// Prohibit moving in negative direction in the lower limit,
					// and prohibit moving in positive direction in the higher limit.
					if ((lowLimit && (Register.Axes_0.Commands.PathPlanner.Xnew <
									  Register.Axes_0.Signals.PositionController.Encoders_0.Position)) ||
						(highLimit && (Register.Axes_0.Commands.PathPlanner.Xnew >
									   Register.Axes_0.Signals.PositionController.Encoders_0.Position))) {

						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.NoCommand;
					}
					break;

				case PathPlannerCommand.MoveDirectCoupled:
				case PathPlannerCommand.MoveCoupled:

					if (lowLimit || highLimit) {

						// Coupling is prohibited.
						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.NoCommand;
					}
					break;

				case PathPlannerCommand.MoveVelocity:
				case PathPlannerCommand.MoveVelocity_Acc:

					// Prohibit moving in negative direction in the lower limit,
					// and prohibit moving in positive direction in the higher limit.
					if ((lowLimit && (Register.Axes_0.Commands.PathPlanner.Vnew < 0f)) ||
						(highLimit && (Register.Axes_0.Commands.PathPlanner.Vnew > 0f))) {

						Register.Axes_0.Commands.PathPlanner.Command = PathPlannerCommand.NoCommand;
					}
					break;

				// not implemented
				//case PathPlannerCommand.MoveRelative:
				//case PathPlannerCommand.MoveRelative_Vel:
				//case PathPlannerCommand.MoveRelative_VelAcc:
				//case PathPlannerCommand.MoveAdditive:
				//case PathPlannerCommand.MoveAdditive_Vel:
				//case PathPlannerCommand.MoveAdditive_VelAcc:
				//case PathPlannerCommand.TorqueControl:
				//case PathPlannerCommand.TorqueControl_Vel:
				//case PathPlannerCommand.TorqueControl_VelAcc:
				//case PathPlannerCommand.CoupleOut:

				// no reaction required
				//case PathPlannerCommand.NoCommand:
				//case PathPlannerCommand.Stop:
				//case PathPlannerCommand.Stop_Acc:
				//case PathPlannerCommand.EmergencyStop:
				//case PathPlannerCommand.SetPosition:
				//case PathPlannerCommand.SetPositionRelative:
				//case PathPlannerCommand.Init:
				default:
					break;
			}
		}
	}
	#endregion Endpoint detection
}

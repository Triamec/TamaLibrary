// Copyright © 2007 Triamec Motion AG

namespace Triamec.Tam.Modules {
	/// <summary>
	///		Commands for the state machine of the isochronous tama 
	///		main program used in the axis of the module catalog.
	/// </summary>
	public static class TamaCommand {
		/// <summary>Idle</summary>
		public const int NoCommand = 0;

		/// <summary>Start homing</summary>
		public const int StartHoming = 1;

		/// <summary>Stop</summary>
		public const int Stop = 2;
	}
}

// Copyright © 2007 Triamec Motion AG

namespace Triamec.Tam.Modules {
	/// <summary>
	///		States for the main state machine of the isochronous Tama
	///		main program used in the axis of the module catalog.
	/// </summary>
	public static class TamaState {
		/// <summary>Idle</summary>
		public const int Idle = 0;

		/// <summary>CheckForHomingAction</summary>
		public const int CheckForHomingAction = 1;

		/// <summary>WaitIndexCleared</summary>
		public const int WaitIndexCleared = 2;

		/// <summary>SearchIndex</summary>
		public const int SearchIndex = 3;

		/// <summary>SetPosition</summary>
		public const int SetPosition = 4;

		/// <summary>WaitPositionSet</summary>
		public const int WaitPositionSet = 5;

		/// <summary>WaitMoveDone</summary>
		public const int WaitMoveDone = 6;
	}
}

// Copyright © 2007 Triamec Motion AG

namespace Triamec.Tam.Modules {
	/// <summary>
	///		States for the main state machine of the isochronous Tama
	///		main program used in the axis of the module catalog.
	/// </summary>
	public static class TamaState {
		/// <summary>Idle</summary>
		public const int Idle = 0;

        /// <summary>TamaProgramRequest</summary>
        public const int Preprocessing = 1;

        /// <summary>TamaProgramRunning</summary>
        public const int Homing = 2;

		/// <summary>SearchIndex</summary>
		public const int Postprocessing = 3;

        /// <summary>Done</summary>
        public const int Done = 4;
    }
}

// Copyright © 2011 Triamec Motion AG

namespace Triamec.Tam.Samples {
    /// <summary>
    /// Register layout independent real-time stopwatch.
    /// </summary>
    /// <remarks><see cref="Tick"/> needs to be called in every cycle.</remarks>
    public class IsochronousTimer {
        #region Fields
        readonly uint _ticksPerSecond;
        uint _tick;
        uint _dest = uint.MaxValue;
		#endregion Fields

		#region Constructor
		/// <summary>
		/// Initializes a new instance of the <see cref="IsochronousTimer"/> class.
		/// </summary>
		/// <param name="sampling">The sampling time, in seconds.</param>
		/// <remarks>If <see cref="Start(float)"/> isn't used, <paramref name="sampling"/> may be omitted.</remarks>
		public IsochronousTimer(float sampling) {
			_ticksPerSecond = (uint)(1f / sampling);
		}
		#endregion Constructor

		#region Methods
		/// <summary>
		/// Starts the timer using the specified duration.
		/// </summary>
		/// <param name="duration">The duration, in seconds.</param>
		public void Start(float duration) {
            _tick = 0;
            _dest = (uint)(duration * _ticksPerSecond);
        }

        /// <summary>
        /// Starts the timer using the specified ticks.
        /// </summary>
        /// <param name="ticks">The ticks, in units of calls to <see cref="Tick"/>.</param>
        public void Start(int ticks) {
            _tick = 0;
            _dest = (uint)ticks;
        }

        /// <summary>
        /// Resets the counter.
        /// </summary>
        public void Reset() => _tick = 0;

        /// <summary>
        /// Increments this timer.
        /// </summary>
        /// <returns>Whether the time has elapsed.</returns>
        /// <remarks>This method must be called periodically according to the provided sampling.</remarks>
        public bool Tick() => ++_tick >= _dest;

        #endregion Methods
    }
}

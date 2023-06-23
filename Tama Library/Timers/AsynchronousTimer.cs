// Copyright © 2011 Triamec Motion AG

namespace Triamec.Tam.Samples {
	/// <summary>
	/// Register layout independent stopwatch.
	/// </summary>
	public class AsynchronousTimer {
		#region Fields
		/// <summary>
		/// The time to wait for, in units of the Tria-Link timestamp.
		/// </summary>
		uint _dest;

        #endregion Fields

        #region Methods
        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name="ticks">The number of ticks, in units of the Tria-Link timestamp.</param>
        /// <param name="currentTime">The current time, in units of the Tria-Link timestamp.</param>
        /// <remarks>
        /// 	<paramref name="ticks"/> must not be greater than <see cref="Timestamp.TimestampTranslator"/>.
        /// </remarks>
        public void Start(int ticks, uint currentTime) => _dest = Timestamp.Add(currentTime, ticks);

        /// <summary>
        /// Starts the timer.
        /// </summary>
        /// <param name="duration">The duration, in seconds.</param>
        /// <param name="currentTime">The current time, in units of the Tria-Link timestamp.</param>
        /// <remarks>This method assumes a Tria-Link tick of 100kHz.</remarks>
        public void Start(float duration, uint currentTime) => Start((int)(duration * 100000), currentTime);

        /// <summary>
        /// Gets a value indicating whether the timer elapsed.
        /// </summary>
        /// <param name="currentTime">The current time, in units of the Tria-Link timestamp.</param>
        /// <returns>
        /// 	<see langword="true"/> if elapsed; otherwise, <see langword="false"/>
        /// </returns>
        /// <remarks>This method may be called at arbitrary points in time.</remarks>
        public bool Elapsed(uint currentTime) => Timestamp.Greater(currentTime, _dest);
        #endregion Methods
    }
}

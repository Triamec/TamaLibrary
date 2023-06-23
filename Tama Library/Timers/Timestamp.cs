// Copyright © 2011 Triamec Motion AG

namespace Triamec.Tam.Samples {
	/// <summary>
	/// A Tria-Link time information class with wrap-around arithmetics.
	/// </summary>
	public static class Timestamp {

		/// <summary>
		/// The range of a timestamp.
		/// <para>The value is <c>0xFFFF'FF00</c>.</para>
		/// </summary>
		/// <remarks>Mathematically, one would write <c>[0..<see cref="TimestampRange"/>)</c>.
		/// <para>The value must be smaller or equal to <see cref="uint.MaxValue"/>.</para>
		/// </remarks>
		const uint TimestampRange = 0xFFFFFF00u;

		/// <summary>
		/// Used for the translation of wrap around transparent calculations.
		/// <para>The value is <c>(<see cref="TimestampRange"/> - 1u) / 2u</c>.</para>
		/// </summary>
		const uint TimestampTranslator = (TimestampRange - 1u) / 2u;

		/// <summary>
		/// Compares two timestamps for equality.
		/// </summary>
		/// <param name="value1">The first timestamp in the comparison.</param>
		/// <param name="value2">The second timestamp in the comparison.</param>
		/// <returns>Returns <see langword="true"/> if <paramref name="value1"/> is greater than <paramref name="value2"/>
		/// with respect to wrapping.
		/// Otherwise, returns <see langword="false"/>.</returns>
		public static bool Greater(uint value1, uint value2) {
			bool wrapProtector = value1 < TimestampTranslator;
			return (value2 < value1 && (wrapProtector || value2 >= value1 - TimestampTranslator)) ||
				(wrapProtector && value2 >= value1 + TimestampTranslator + 2);
		}

		/// <summary>
		/// Compares two timestamps for equality.
		/// </summary>
		/// <param name="value1">The first timestamp in the comparison.</param>
		/// <param name="value2">The second timestamp in the comparison.</param>
		/// <returns>Returns <see langword="true"/> if <paramref name="value1"/> is greater or equal than <paramref name="value2"/>
		/// with respect to wrapping.
		/// Otherwise, returns <see langword="false"/>.</returns>
		public static bool GreaterOrEqual(uint value1, uint value2) {
			bool wrapProtector = value1 < TimestampTranslator;
			return (value2 <= value1 && (wrapProtector || value2 >= value1 - TimestampTranslator)) ||
				(wrapProtector && value2 >= value1 + TimestampTranslator + 2);
		}

		/// <summary>
		/// Adds a duration to a timestamp.
		/// </summary>
		/// <param name="timestamp">The augend.</param>
		/// <param name="duration">The addend.</param>
		public static uint Add(uint timestamp, int duration) {
			uint result;
			if (duration < 0) {
				result = Subtract(timestamp, -(duration + 1)) - 1;  // unsymmetrical range mapping
			} else {
				uint positiveDuration = (uint)duration;
				uint temp = TimestampRange - positiveDuration;
				if (timestamp < temp) {
					result = timestamp + positiveDuration;
				} else {
					result = timestamp - temp;
				}
			}
			return result;
		}

		/// <summary>
		/// Subtracts a duration from a timestamp.
		/// </summary>
		/// <param name="timestamp">The minuend.</param>
		/// <param name="duration">The subtrahend.</param>
		private static uint Subtract(uint timestamp, int duration) {
			// duration must be greater than zero
			uint result;
			uint positiveDuration = (uint)duration;
			if (timestamp >= positiveDuration) {
				// subtraction equal or greater than 0
				result = timestamp - positiveDuration;
			} else {
				uint temp = TimestampRange - positiveDuration;
				result = timestamp + temp;
			}
			return result;
		}
	}
}

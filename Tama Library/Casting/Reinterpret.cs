// Copyright © 2015 Triamec Motion AG

namespace Triamec.Tam.Samples {
	/// <summary>
	/// Provides casts resembling the <c>reinterpret_cast</c> in C++.
	/// </summary>
	/// <remarks>
	///   <para>Use these casts when working with registers where, in the TAM system explorer,
	/// a pop-down allows to choose the <see cref="Registers.UniversalRegisterType"/>.</para>
	///   <para>Integrating this class requires the <c>/unsafe</c> switch. Therefore, you'll need to enable the switch
	/// in the <c>General</c> section of the <c>Build</c> tab in the project properties. The desktop link doesn't
	/// support unsafe code.</para>
	/// </remarks>
	public static class Reinterpret {
        /// <summary>
        /// Returns the 32-bit signed integer representation of a single precision floating point value in memory.
        /// </summary>
        public static unsafe int AsInteger(float value) => *((int*)(void*)&value);

        /// <summary>
        /// Returns the single precision floating point representation of a 32-bit signed integer value in memory.
        /// </summary>
        public static unsafe float AsFloat(int value) => *((float*)(void*)&value);
    }
}

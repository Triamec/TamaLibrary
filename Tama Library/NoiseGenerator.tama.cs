// Copyright © 2014 Triamec Motion AG

using Triamec.Tama.Rlid19;
using Triamec.Tama.Vmid5;
using Triamec.TriaLink;
using Triamec.Tam.Samples;

namespace Triamec.Tam.Samples {
	/// <summary>
	/// Noise generator which generates uniformly distributed pseudorandom numbers in the interval
	/// <c>(1/<see cref="M"/>,1-1/<see cref="M"/>)</c>.
	/// </summary>
	/// <remarks>
	/// The implementation is based on Schrage's method which is based on the Park-Miller Algorithm.
	/// </remarks>
	sealed class SchragesRandGen {

		// definition of constants - do not modify!
		const int A = 48271;
		const int M = 2147483647;
		const int Q = M / A;
		const int R = M - Q * A; //equivalent to mod(m,a)

		int _seed = 1;

		// random number generator
		public float Next() {
			int hi = _seed / Q;
			int lo = _seed - hi * Q; // equivalent to mod(seed,q)

			int test = A * lo - R * hi;

			_seed = test > 0 ? test : test + M;

			return (float)_seed / M;
		}
	}
}

/// <summary>
/// Generates a normal distributed random signal with a configurable RMS-value.
/// </summary>
/// <remarks>
/// <see cref="TamaVariables.GenPurposeVar0"/> is the input to configure RMS value.
/// <see cref="TamaVariables.GenPurposeVar24"/> is the output of the random signal.
/// <note type="caution">The used algorithm is relatively expensive.</note>
/// </remarks>
[Tama]
static class NoiseGenerator {

	// definition of constants
	const float Twoπ = 2 * Math.PI;

	static readonly SchragesRandGen Srg = new SchragesRandGen();

	[TamaTask(Task.IsochronousMain)]
	public static void IsochronApplication() {

		if (Register.Axes_0.Signals.General.AxisState >= AxisState.Standstill &&
			Register.Axes_0.Signals.General.AxisState <= AxisState.TamaCoupledMotion) {

			// random values (uniformly distributed in interval (0,1))
			float u1 = Srg.Next();
			float u2 = Srg.Next();

			// calculate normal distributed signal based on the Marsaglia polar method
			float result = Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(Twoπ * u2);

			// multiply signal with the desired RMS value and write the noise value to the
			// tama variable
			Register.Application.Variables.Floats[24] = result * Register.Application.Variables.Floats[0];
		}
	}
}

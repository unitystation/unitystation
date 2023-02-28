using UnityEngine;

namespace Core.Utils
{
	/// <summary>
	/// Collection of miscellaneous math utility functions.
	/// <seealso cref="DMMath"/>
	/// <seealso cref="RandomUtils"/>
	/// </summary>
	public class MathUtils
	{
		/// <summary>Check if a float is approximately equal to another.</summary>
		public static bool IsEqual(float a, float b)
		{
			return a >= b - Mathf.Epsilon && a <= b + Mathf.Epsilon;
		}

		/// <summary>
		/// Calculate modulo.
		/// With most languages "x % m" actually calculates the remainder and so is different to the modulo for negative x.
		/// <see href="https://en.wikipedia.org/wiki/Modulo">See Wikipedia's article on this.</see>
		/// </summary>
		public static int Mod(int x, int m)
		{
			return (x % m + m) % m;
		}
	}
}

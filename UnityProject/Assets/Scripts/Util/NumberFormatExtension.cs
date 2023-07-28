using System;

// Many thanks to StackOverflow for a lot of this code. Used the best parts of various answers

public static class NumberFormatExtension
{
	/// <summary>
	/// Converts a number to a string with an SI unit prefix and an optional unit e.g. k, M, G, μ etc...
	/// </summary>
	/// <param name="value">The number to convert</param>
	/// <param name="unit">The unit to use</param>
	public static string ToEngineering(this int value, string unit = "")
	{
		return ToEngineering((double)value, unit);
	}
	/// <summary>
	/// Converts a number to a string with an SI unit prefix and an optional unit e.g. k, M, G, μ etc...
	/// </summary>
	/// <param name="value">The number to convert</param>
	/// <param name="unit">The unit to use</param>
	public static string ToEngineering(this float value, string unit = "")
	{
		return ToEngineering((double)value, unit);
	}

	/// <summary>
	/// Converts a number to a string with an SI unit prefix and an optional unit e.g. k, M, G, μ etc...
	/// </summary>
	/// <param name="value">The number to convert</param>
	/// <param name="unit">The unit to use</param>
	public static string ToEngineering(this double value, string unit = "")
	{
		if (double.IsNaN(value) ||
		    double.IsInfinity(value) ||
		    double.IsNegativeInfinity(value) ||
		    double.IsPositiveInfinity(value))
		{
			// This should return the infinity symbol with the optional unit
			return string.IsNullOrEmpty(unit) ? "\u221E" : $"\u221E {unit}";
		}

		// Calculate the exponent
		int exponent = (int)(Math.Floor( Math.Log10(value) / 3.0 ) * 3.0);

		// Calculate the new value after accounting for the exponent
		double newValue = value * Math.Pow(10.0, -exponent);
		if (newValue >= 1000.0)
		{
			newValue = newValue / 1000.0;
			exponent      = exponent + 3;
		}

		// Determine the appropriate SI unit prefix
		string prefix = " ";
		if (exponent >= 0)
		{
			switch (exponent)
			{
				case 0: case 1: case 2:
					break;
				case 3: case 4: case 5:
					prefix = "k";
					break;
				case 6: case 7: case 8:
					prefix = "M";
					break;
				case 9: case 10: case 11:
					prefix = "G";
					break;
				case 12: case 13: case 14:
					prefix = "T";
					break;
				case 15: case 16: case 17:
					prefix = "P";
					break;
				case 18: case 19: case 20:
					prefix = "E";
					break;
				case 21: case 22: case 23:
					prefix = "Z";
					break;
				default:
					prefix = "Y";
					break;
			}
		}
		else if (exponent < 0)
		{
			switch (exponent)
			{
				case -1: case -2: case -3:
					prefix = "m";
					break;
				case -4: case -5: case -6:
					prefix = "μ";
					break;
				case -7: case -8: case -9:
					prefix = "n";
					break;
				case -10: case -11: case -12:
					prefix = "p";
					break;
				case -13: case -14: case -15:
					prefix = "f";
					break;
				case -16: case -17: case -18:
					prefix = "a";
					break;
				case -19: case -20: case -21:
					prefix = "z";
					break;
				default:
					prefix = "y";
					break;
			}
		}

		return $"{newValue:##0.#} {prefix}{unit}";
	}
}
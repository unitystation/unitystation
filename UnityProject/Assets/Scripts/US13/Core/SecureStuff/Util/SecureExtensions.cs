using System;
using System.Globalization;
using Logs;
using Newtonsoft.Json;

namespace SecureStuff.Util
{
	public static class SecureExtensions
	{
		/// <summary>
		/// Parses a string into a given type.
		/// </summary>
		/// <param name="str">content to be parssed.</param>
		/// <returns>The type of content that was parsed, as well as it's parsed object.</returns>
		public static Tuple<Type, object> ParseString(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				Loggy.Warning("Attempted to deserialize info, but there was nothing.");
				return null;
			}

			if (int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
				return new Tuple<Type, object>(typeof(int), intValue);

			if (float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
				return new Tuple<Type, object>(typeof(float), floatValue);

			if (double.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
				return new Tuple<Type, object>(typeof(double), doubleValue);

			if (bool.TryParse( str, out var boolValue))
				return new Tuple<Type, object>(typeof(bool),boolValue);

			if (decimal.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
				return new Tuple<Type, object>(typeof(decimal), decimalValue);

			if (long.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
				return new Tuple<Type, object>(typeof(long), longValue);

			if (short.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue))
				return new Tuple<Type, object>(typeof(short), shortValue);

			if (byte.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
				return new Tuple<Type, object>(typeof(byte), byteValue);

			return new Tuple<Type, object>(typeof(string) ,str);
		}
	}
}
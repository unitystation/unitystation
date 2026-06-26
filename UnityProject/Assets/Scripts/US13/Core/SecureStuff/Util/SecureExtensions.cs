using System;
using System.Globalization;
using Logs;
using Newtonsoft.Json;

namespace SecureStuff.Util
{
	public static class SecureExtensions
	{
		/// <summary>
		/// Parses a string into a given type. If the string is null or empty, returns the default value for that type.
		/// Can be used to directly parse strings into primitive types or to deserialize JSON into objects.
		/// </summary>
		/// <param name="str">content to be parsed</param>
		/// <param name="defaultValue">default value to return if parsing fails</param>
		/// <typeparam name="T">Expected type</typeparam>
		/// <returns>Parsed value from string.</returns>
		public static T ParseString<T>(this string str, T defaultValue = default)
		{
			if (string.IsNullOrWhiteSpace(str))
			{
				Loggy.Warning("Attempted to deserialize info, but there was nothing.");
				return default;
			}

			var targetType = typeof(T);

			if (targetType == typeof(int) && int.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
				return (T)(object)intValue;

			if (targetType == typeof(float) && float.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
				return (T)(object)floatValue;

			if (targetType == typeof(double) && double.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
				return (T)(object)doubleValue;

			if (targetType == typeof(bool) && bool.TryParse( str, out var boolValue))
				return (T)(object)boolValue;

			if (targetType == typeof(decimal) && decimal.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
				return (T)(object)decimalValue;

			if (targetType == typeof(long) && long.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
				return (T)(object)longValue;

			if (targetType == typeof(short) && short.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue))
				return (T)(object)shortValue;

			if (targetType == typeof(byte) && byte.TryParse( str, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
				return (T)(object)byteValue;

			if (targetType == typeof(string))
				return (T)(object) str;

			try
			{
				return JsonConvert.DeserializeObject<T>(str);
			}
			catch (Exception e)
			{
				Loggy.Error($"An issue has happened while trying to Deserialize an Object.\n Value: { str}\n\n {e}");
				return default;
			}
		}

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
				return default;
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
using System;
using System.Globalization;
using Logs;
using Mirror;
using Newtonsoft.Json;

namespace Core.Networking.AsyncMessageQueue
{
	public enum MessageStatus
	{
		Success = 0,
		Failure = 1,
	}

	public class QueuedMessage
	{
		public string ValueFromJson;
		public NetworkIdentity Requester;
		public MessageStatus Status;

		public T DeserializeFromText<T>()
		{
			if (string.IsNullOrWhiteSpace(ValueFromJson))
			{
				Loggy.Warning("Attempted to deserialize info, but there was nothing.");
				return default;
			}

			var targetType = typeof(T);

			if (targetType == typeof(int) && int.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
				return (T)(object)intValue;

			if (targetType == typeof(float) && float.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
				return (T)(object)floatValue;

			if (targetType == typeof(double) && double.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
				return (T)(object)doubleValue;

			if (targetType == typeof(bool) && bool.TryParse(ValueFromJson, out var boolValue))
				return (T)(object)boolValue;

			if (targetType == typeof(decimal) && decimal.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
				return (T)(object)decimalValue;

			if (targetType == typeof(long) && long.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
				return (T)(object)longValue;

			if (targetType == typeof(short) && short.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue))
				return (T)(object)shortValue;

			if (targetType == typeof(byte) && byte.TryParse(ValueFromJson, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
				return (T)(object)byteValue;

			if (targetType == typeof(string))
				return (T)(object)ValueFromJson;

			try
			{
				return JsonConvert.DeserializeObject<T>(ValueFromJson);
			}
			catch (Exception e)
			{
				Loggy.Error($"An issue has happened while trying to Deserialize an Object.\n Value: {ValueFromJson}\n\n {e}");
				return default;
			}
		}
	}
}
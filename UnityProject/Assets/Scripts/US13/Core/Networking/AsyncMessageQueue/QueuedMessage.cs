using Mirror;
using Newtonsoft.Json;
using SecureStuff.Util;

namespace US13.Core.Networking.AsyncMessageQueue
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
			try
			{
				return JsonConvert.DeserializeObject<T>(ValueFromJson);
			}
			catch
			{
				var result = ValueFromJson.ParseString();
				return (T)result.Item2;
			}
			return default;
		}
	}
}
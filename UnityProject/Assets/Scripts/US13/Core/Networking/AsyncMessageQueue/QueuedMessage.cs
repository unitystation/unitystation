using Mirror;
using Util;

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
			return ValueFromJson.ParseString<T>();
		}
	}
}
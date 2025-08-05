using Mirror;

namespace Core.Networking.AsyncMessageQueue
{
	/// <summary>
	/// Server to Client message containing the result of the async request
	/// </summary>
	public class AsyncDataResponse : Messages.Server.ServerMessage<AsyncDataResponse.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string Token;
			public string ValueFromJson;
			public MessageStatus Status;
		}

		public override void Process(NetMessage msg)
		{
			if (RpcMessageQueue.Instance == null) return;

			var queuedMessage = new QueuedMessage
			{
				Requester = null, // Will be set by the queue system
				Status = msg.Status,
				ValueFromJson = msg.ValueFromJson
			};

			RpcMessageQueue.Instance.ProcessResponse(msg.Token, queuedMessage);
		}

		public static void SendTo(NetworkConnection recipient, string token, QueuedMessage result)
		{
			var netMessage = new NetMessage
			{
				Token = token,
				ValueFromJson = result.ValueFromJson,
				Status = result.Status
			};

			SendTo(recipient, netMessage);
		}
	}
}
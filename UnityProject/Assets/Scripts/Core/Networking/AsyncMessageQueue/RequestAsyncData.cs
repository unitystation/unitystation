using Mirror;

namespace Core.Networking.AsyncMessageQueue
{
	/// <summary>
	/// Client to Server message requesting data from a registered handler
	/// </summary>
	public class RequestAsyncData : Messages.Client.ClientMessage<RequestAsyncData.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public string RequestedTicket;
			public string Token;
		}

		public override void Process(NetMessage msg)
		{
			if (RpcMessageQueue.Instance == null) return;

			RpcMessageQueue.Instance.ProcessRequest(msg.RequestedTicket, msg.Token, SentBy);
		}

		public static void Send(string requestedTicket, string token)
		{
			var netMessage = new NetMessage
			{
				RequestedTicket = requestedTicket,
				Token = token
			};

			Send(netMessage);
		}
	}
}
using Messages.Server;
using Mirror;

namespace Messages.Client
{
	public class RequestHighlanderTime : ClientMessage<RequestHighlanderTime.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ConnectedPlayer ConnectedPlayer;
		}

		public override void Process(NetMessage msg)
		{
			HighlanderTimerMessage.Send(msg.ConnectedPlayer);
		}

		public static NetMessage Send(ConnectedPlayer player)
		{
			var msg = new NetMessage()
			{
				ConnectedPlayer = player
			};

			Send(msg);
			return msg;
		}
	}
}
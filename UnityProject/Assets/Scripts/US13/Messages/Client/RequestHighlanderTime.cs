using Mirror;
using US13.Managers;
using US13.Messages.Server;

namespace US13.Messages.Client
{
	public class RequestHighlanderTime : ClientMessage<RequestHighlanderTime.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			// TODO: don't send whole class objects (GC issues and PlayerInfo is not tiny)
			public PlayerInfo ConnectedPlayer;
		}

		public override void Process(NetMessage msg)
		{
			HighlanderTimerMessage.Send(msg.ConnectedPlayer);
		}

		public static NetMessage Send(PlayerInfo player)
		{
			var msg = new NetMessage
			{
				ConnectedPlayer = player
			};

			Send(msg);
			return msg;
		}
	}
}

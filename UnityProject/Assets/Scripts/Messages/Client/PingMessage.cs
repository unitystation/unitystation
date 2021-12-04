using Mirror;
using UnityEngine;

namespace Messages.Client
{

	public class PingMessage : ClientMessage<PingMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }
		public override void Process(NetMessage msg)
		{
			Server.UpdateConnectedPlayersMessage.Send();
		}

		public static NetMessage Send()
		{
			NetMessage msg = new NetMessage();
			Send(msg);
			return msg;
		}
	}
}

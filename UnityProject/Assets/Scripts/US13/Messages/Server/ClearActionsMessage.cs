using Mirror;
using UnityEngine;
using US13.Actions;

namespace US13.Messages.Server
{
	public class ClearActionsMessage : ServerMessage<ClearActionsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			UIActionManager.ClearAllActionsClient();
		}

		public static NetMessage SendTo(GameObject recipient)
		{
			NetMessage msg = new NetMessage {};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
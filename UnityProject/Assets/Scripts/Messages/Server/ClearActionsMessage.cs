using Mirror;
using UI.Action;
using UnityEngine;

namespace Messages.Server
{
	public class ClearActionsMessage : ServerMessage<ClearActionsMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage { }

		public override void Process(NetMessage msg)
		{
			UIActionManager.ClearAllActions();
		}

		public static NetMessage SendTo(GameObject recipient)
		{
			NetMessage msg = new NetMessage {};

			SendTo(recipient, msg);
			return msg;
		}
	}
}
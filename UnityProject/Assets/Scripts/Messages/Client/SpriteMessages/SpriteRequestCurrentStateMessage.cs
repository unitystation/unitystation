using Mirror;

namespace Messages.Client.SpriteMessages
{
	public class SpriteRequestCurrentStateMessage : ClientMessage<SpriteRequestCurrentStateMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint SpriteHandlerManager;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.SpriteHandlerManager);
			if (SentByPlayer == ConnectedPlayer.Invalid)
				return;

			NetworkObject.GetComponent<SpriteHandlerManager>().UpdateNewPlayer(SentByPlayer.Connection);
		}

		public static NetMessage Send(uint spriteHandlerManager)
		{
			var msg = new NetMessage()
			{
				SpriteHandlerManager = spriteHandlerManager
			};

			Send(msg);
			return msg;
		}
	}
}

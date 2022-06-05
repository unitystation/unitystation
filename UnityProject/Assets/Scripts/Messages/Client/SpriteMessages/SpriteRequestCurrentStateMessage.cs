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
			if (SentByPlayer == PlayerInfo.Invalid)
				return;

		LoadNetworkObject(msg.SpriteHandlerManager);
		if (SentByPlayer == PlayerInfo.Invalid)
			return;
		//TODO Need some safeguards
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

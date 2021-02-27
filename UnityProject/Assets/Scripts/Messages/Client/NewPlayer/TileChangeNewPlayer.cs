using Mirror;

namespace Messages.Client.NewPlayer
{
	public class TileChangeNewPlayer : ClientMessage<TileChangeNewPlayer.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint TileChangeManager;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.TileChangeManager);
			NetworkObject.GetComponent<TileChangeManager>().UpdateNewPlayer(
				SentByPlayer.Connection);
		}

		public static NetMessage Send(uint tileChangeNetId)
		{
			NetMessage msg = new NetMessage
			{
				TileChangeManager = tileChangeNetId
			};

			Send(msg);
			return msg;
		}
	}
}

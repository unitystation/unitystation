using System.Collections;
using Messages.Client;
using Mirror;

public class TileChangeNewPlayer : ClientMessage
{
	public class TileChangeNewPlayerNetMessage : NetworkMessage
	{
		public uint TileChangeManager;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as TileChangeNewPlayerNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.TileChangeManager);
		NetworkObject.GetComponent<TileChangeManager>().UpdateNewPlayer(
			SentByPlayer.Connection);
	}

	public static TileChangeNewPlayerNetMessage Send(uint tileChangeNetId)
	{
		TileChangeNewPlayerNetMessage msg = new TileChangeNewPlayerNetMessage
		{
			TileChangeManager = tileChangeNetId
		};
		new TileChangeNewPlayer().Send(msg);
		return msg;
	}
}

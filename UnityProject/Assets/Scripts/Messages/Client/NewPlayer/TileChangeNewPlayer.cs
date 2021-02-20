using System.Collections;
using Messages.Client;

public class TileChangeNewPlayer : ClientMessage
{
	public class TileChangeNewPlayerNetMessage : ActualMessage
	{
		public uint TileChangeManager;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as TileChangeNewPlayerNetMessage;
		if(newMsg == null) return;

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

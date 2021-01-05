using System.Collections;
using Messages.Client;

public class TileChangeNewPlayer: ClientMessage
{
	public uint TileChangeManager;

	public override void Process()
	{
		LoadNetworkObject(TileChangeManager);
		NetworkObject.GetComponent<TileChangeManager>().UpdateNewPlayer(
			SentByPlayer.Connection);
	}

	public static TileChangeNewPlayer Send(uint tileChangeNetId)
	{
		TileChangeNewPlayer msg = new TileChangeNewPlayer
		{
			TileChangeManager = tileChangeNetId
		};
		msg.Send();
		return msg;
	}
}

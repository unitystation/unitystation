using System.Collections;
using Mirror;

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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		TileChangeManager = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(TileChangeManager);
	}
}
using System.Collections;
using Mirror;

public class DoorNewPlayer: ClientMessage
{
	public uint Door;

	public override void Process()
	{
		LoadNetworkObject(Door);
		NetworkObject.GetComponent<DoorController>().UpdateNewPlayer(
			SentByPlayer.Connection);
	}

	public static DoorNewPlayer Send(uint netId)
	{
		DoorNewPlayer msg = new DoorNewPlayer
		{
			Door = netId
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Door = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Door);
	}
}
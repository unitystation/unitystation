using System.Collections;
using Mirror;

public class CustomNetTransformNewPlayer: ClientMessage
{
	public uint CNT;

	public override void Process()
	{
		LoadNetworkObject(CNT);
		NetworkObject.GetComponent<CustomNetTransform>().NotifyPlayer(
			SentByPlayer.Connection);
	}

	public static CustomNetTransformNewPlayer Send(uint netId)
	{
		CustomNetTransformNewPlayer msg = new CustomNetTransformNewPlayer
		{
			CNT = netId
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		CNT = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(CNT);
	}
}
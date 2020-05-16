using System.Collections;
using Mirror;

public class PlayerNewPlayer: ClientMessage
{
	public uint Player;

	public override void Process()
	{
		LoadNetworkObject(Player);
		NetworkObject.GetComponent<PlayerSync>().NotifyPlayer(
			SentByPlayer.Connection);
		NetworkObject.GetComponent<PlayerSprites>().NotifyPlayer(
			SentByPlayer.Connection);
		NetworkObject.GetComponent<Equipment>().NotifyPlayer(
			SentByPlayer.Connection);
	}

	public static PlayerNewPlayer Send(uint netId)
	{
		PlayerNewPlayer msg = new PlayerNewPlayer
		{
			Player = netId
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Player = reader.ReadUInt32();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteUInt32(Player);
	}
}
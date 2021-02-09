using System.Collections;
using Messages.Client;

public class PlayerNewPlayer: ClientMessage
{
	public uint Player;

	public override void Process()
	{
		LoadNetworkObject(Player);
		if (NetworkObject == null) return;
		NetworkObject.GetComponent<PlayerSync>()?.NotifyPlayer(
			SentByPlayer.Connection);
		NetworkObject.GetComponent<PlayerSprites>()?.NotifyPlayer(
			SentByPlayer.Connection);
		NetworkObject.GetComponent<Equipment>()?.NotifyPlayer(
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
}

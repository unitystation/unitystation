using System.Collections;
using Messages.Client;

public class PlayerNewPlayer : ClientMessage
{
	public class PlayerNewPlayerNetMessage : ActualMessage
	{
		public uint Player;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as PlayerNewPlayerNetMessage;
		if(newMsg == null) return;

		LoadNetworkObject(newMsg.Player);
		if (NetworkObject == null) return;
		NetworkObject.GetComponent<PlayerSync>()?.NotifyPlayer(
			SentByPlayer.Connection);
		NetworkObject.GetComponent<PlayerSprites>()?.NotifyPlayer(
			SentByPlayer.Connection);
		NetworkObject.GetComponent<Equipment>()?.NotifyPlayer(
			SentByPlayer.Connection);
	}

	public static PlayerNewPlayerNetMessage Send(uint netId)
	{
		PlayerNewPlayerNetMessage msg = new PlayerNewPlayerNetMessage
		{
			Player = netId
		};
		new PlayerNewPlayer().Send(msg);
		return msg;
	}
}

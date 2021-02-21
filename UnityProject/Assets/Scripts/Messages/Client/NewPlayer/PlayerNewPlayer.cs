using System.Collections;
using Messages.Client;
using Mirror;

public class PlayerNewPlayer : ClientMessage
{
	public class PlayerNewPlayerNetMessage : NetworkMessage
	{
		public uint Player;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PlayerNewPlayerNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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

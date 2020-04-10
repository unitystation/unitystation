using System.Collections;
using UnityEngine;
using Mirror;

public class PlayerDeathMessage : ServerMessage
{
	public override void Process()
	{
		OnYourDeath();
	}

/// What should client do then he receives a message that he's dead
	private void OnYourDeath()
	{
		PlayerScript localPlayerScript = PlayerManager.LocalPlayerScript;
		EventManager.Broadcast(EVENT.PlayerDied);
	}

	///     Sends the death message
	public static PlayerDeathMessage Send(GameObject recipient)
	{
		PlayerDeathMessage msg = new PlayerDeathMessage();
		msg.SendTo(recipient);
		return msg;
	}
}
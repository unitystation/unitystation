using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class PlayerDeathMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.PlayerDeathMessage;


	public override IEnumerator Process()
	{
		yield return null;
		OnYourDeath();
	}

/// What should client do then he receives a message that he's dead
	private void OnYourDeath()
	{
		PlayerScript localPlayerScript = PlayerManager.LocalPlayerScript;
//		localPlayerScript.playerNetworkActions.SpawnGhost();
		localPlayerScript.PlayerSync.OnBecomeGhost();
		UIManager.SetDeathVisibility( false );
	}

	///     Sends the death message
	public static PlayerDeathMessage Send(GameObject recipient)
	{
		PlayerDeathMessage msg = new PlayerDeathMessage();
		msg.SendTo(recipient);
		return msg;
	}
}
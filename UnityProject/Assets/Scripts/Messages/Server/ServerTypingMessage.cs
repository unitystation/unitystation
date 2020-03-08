using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Message from server to client that indicate that other player is typing
/// Sends only to player that's are nearby to speaker
/// </summary>
public class ServerTypingMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.ServerTypingMessage;

	public TypingState state;
	public uint targetID;

	public override IEnumerator Process()
	{
		// other client try to find networked identity that's typing
		yield return WaitFor(targetID);
		if (!NetworkObject)
			yield break;

		// than we change it typing icon
		var player = NetworkObject.GetComponent<PlayerScript>();
		if (!player)
			yield break;

		var icon = player.chatIcon;
		if (!icon)
			yield break;

		var showTyping = state == TypingState.TYPING;
		icon.ToggleTypingIcon(showTyping);
	}

	public static ServerTypingMessage Send(PlayerScript player, TypingState state)
	{
		var msg = new ServerTypingMessage()
		{
			state = state,
			targetID = player.netId
		};

		var playerPos = player.transform.position;
		msg.SendToNearbyPlayers(playerPos);
		return msg;
	}
}

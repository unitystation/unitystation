using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServerTypingMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.ServerTypingMessage;

	public TypingState state;
	public uint targetID;

	public override IEnumerator Process()
	{
		yield return WaitFor(targetID);

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

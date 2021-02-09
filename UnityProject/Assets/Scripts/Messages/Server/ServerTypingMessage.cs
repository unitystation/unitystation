using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Message from server to client that indicate that other player is typing
/// Sends only to player that's are nearby to speaker
/// </summary>
public class ServerTypingMessage : ServerMessage
{
	public TypingState state;
	public uint targetID;

	public override void Process()
	{
		// other client try to find networked identity that's typing
		LoadNetworkObject(targetID);
		if (!NetworkObject)
			return;

		// than we change it typing icon
		var player = NetworkObject.GetComponent<PlayerScript>();
		if (!player)
			return;

		var icon = player.chatIcon;
		if (!icon)
			return;

		var showTyping = state == TypingState.TYPING;

		// check if player is conscious before generating typing icon
		bool isPlayerConscious = (player.playerHealth.ConsciousState == ConsciousState.CONSCIOUS ||
								  player.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS);
		if (isPlayerConscious)
		{
			icon.ToggleTypingIcon(showTyping);
		}
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

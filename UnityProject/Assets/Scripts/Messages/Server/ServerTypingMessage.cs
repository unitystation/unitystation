using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Message from server to client that indicate that other player is typing
/// Sends only to player that's are nearby to speaker
/// </summary>
public class ServerTypingMessage : ServerMessage
{
	public class ServerTypingMessageNetMessage : NetworkMessage
	{
		public TypingState state;
		public uint targetID;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as ServerTypingMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		// other client try to find networked identity that's typing
		LoadNetworkObject(newMsg.targetID);
		if (!NetworkObject)
			return;

		// than we change it typing icon
		var player = NetworkObject.GetComponent<PlayerScript>();
		if (!player)
			return;

		var icon = player.chatIcon;
		if (!icon)
			return;

		var showTyping = newMsg.state == TypingState.TYPING;

		// check if player is conscious before generating typing icon
		bool isPlayerConscious = (player.playerHealth.ConsciousState == ConsciousState.CONSCIOUS ||
								  player.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS);
		if (isPlayerConscious)
		{
			icon.ToggleTypingIcon(showTyping);
		}
	}

	public static ServerTypingMessageNetMessage Send(PlayerScript player, TypingState state)
	{
		var msg = new ServerTypingMessageNetMessage()
		{
			state = state,
			targetID = player.netId
		};

		var playerPos = player.transform.position;
		new ServerTypingMessage().SendToNearbyPlayers(playerPos, msg);
		return msg;
	}
}

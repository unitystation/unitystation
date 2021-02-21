using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Sends a message to ChatBubbleManager on the client to show a chat bubble
/// </summary>
public class ShowChatBubbleMessage : ServerMessage
{
	public struct ShowChatBubbleMessageNetMessage : NetworkMessage
	{
		public ChatModifier ChatModifiers;
		public string Message;
		public uint FollowTransform;
		public bool IsPlayerChatBubble;

		//Special flag for finding the correct transform target on players
		//You may have to do something like this if your target does not
		//have a NetworkIdentity on it
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public ShowChatBubbleMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as ShowChatBubbleMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		LoadNetworkObject(newMsg.FollowTransform);
		var target = NetworkObject.transform;

		if (newMsg.IsPlayerChatBubble)
		{
			target = target.GetComponent<PlayerNetworkActions>().chatBubbleTarget;
		}

		ChatBubbleManager.ShowAChatBubble(target, newMsg.Message, newMsg.ChatModifiers);
	}

	public static ShowChatBubbleMessageNetMessage SendToNearby(GameObject followTransform, string message, bool isPlayerChatBubble = false,
		ChatModifier chatModifier = ChatModifier.None)
	{
		ShowChatBubbleMessageNetMessage msg = new ShowChatBubbleMessageNetMessage
		{
			ChatModifiers = chatModifier,
			Message = message,
			FollowTransform = followTransform.GetComponent<NetworkIdentity>().netId,
			IsPlayerChatBubble = isPlayerChatBubble
		};

		new ShowChatBubbleMessage().SendToVisiblePlayers(followTransform.transform.position, msg);
		return msg;
	}
}
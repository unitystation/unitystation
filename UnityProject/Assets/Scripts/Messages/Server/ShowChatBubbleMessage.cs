using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Sends a message to ChatBubbleManager on the client to show a chat bubble
/// </summary>
public class ShowChatBubbleMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.ShowChatBubble;
	public ChatModifier ChatModifiers;
	public string Message;
	public uint FollowTransform;
	public bool IsPlayerChatBubble; //Special flag for finding the correct transform target on players
									//You may have to do something like this if your target does not
									//have a NetworkIdentity on it

	public override IEnumerator Process()
	{
		yield return WaitFor(FollowTransform);
		var target = NetworkObject.transform;

		if (IsPlayerChatBubble)
		{
			target = target.GetComponent<PlayerNetworkActions>().chatBubbleTarget;
		}

		ChatBubbleManager.ShowAChatBubble(target, Message, ChatModifiers);
	}

	public static ShowChatBubbleMessage SendToNearby(GameObject followTransform, string message, bool isPlayerChatBubble = false,
		ChatModifier chatModifier = ChatModifier.None)
	{
		ShowChatBubbleMessage msg = new ShowChatBubbleMessage
		{
			ChatModifiers = chatModifier,
			Message = message,
			FollowTransform = followTransform.GetComponent<NetworkIdentity>().netId,
			IsPlayerChatBubble = isPlayerChatBubble
		};

		msg.SendToVisiblePlayers(followTransform.transform.position);
		return msg;
	}
}
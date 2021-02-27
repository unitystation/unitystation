using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Sends a message to ChatBubbleManager on the client to show a chat bubble
	/// </summary>
	public class ShowChatBubbleMessage : ServerMessage<ShowChatBubbleMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public ChatModifier ChatModifiers;
			public string Message;
			public uint FollowTransform;
			public bool IsPlayerChatBubble;

			//Special flag for finding the correct transform target on players
			//You may have to do something like this if your target does not
			//have a NetworkIdentity on it
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.FollowTransform);
			var target = NetworkObject.transform;

			if (msg.IsPlayerChatBubble)
			{
				target = target.GetComponent<PlayerNetworkActions>().chatBubbleTarget;
			}

			ChatBubbleManager.ShowAChatBubble(target, msg.Message, msg.ChatModifiers);
		}

		public static NetMessage SendToNearby(GameObject followTransform, string message, bool isPlayerChatBubble = false,
			ChatModifier chatModifier = ChatModifier.None)
		{
			NetMessage msg = new NetMessage
			{
				ChatModifiers = chatModifier,
				Message = message,
				FollowTransform = followTransform.GetComponent<NetworkIdentity>().netId,
				IsPlayerChatBubble = isPlayerChatBubble
			};

			SendToVisiblePlayers(followTransform.transform.position, msg);
			return msg;
		}
	}
}
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
			public bool AllowTags;

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

			var message = msg.Message;

			if(msg.AllowTags == false)
			{
				message = Chat.StripTags(message);
			}

			ChatBubbleManager.ShowAChatBubble(target, message, msg.ChatModifiers);
		}

		public static NetMessage SendToNearby(GameObject followTransform, string message, bool isPlayerChatBubble = false,
			ChatModifier chatModifier = ChatModifier.None, bool allowTags = false)
		{
			NetMessage msg = new NetMessage
			{
				ChatModifiers = chatModifier,
				Message = message,
				FollowTransform = followTransform.GetComponent<NetworkIdentity>().netId,
				IsPlayerChatBubble = isPlayerChatBubble,
				AllowTags = allowTags
			};

			SendToVisiblePlayers(followTransform.transform.position, msg);
			return msg;
		}

		public static NetMessage SendTo(NetworkConnectionToClient conn, GameObject followTransform, string message, bool isPlayerChatBubble = false,
			ChatModifier chatModifier = ChatModifier.None, bool allowTags = false)
		{
			NetMessage msg = new NetMessage
			{
				ChatModifiers = chatModifier,
				Message = message,
				FollowTransform = followTransform.GetComponent<NetworkIdentity>().netId,
				IsPlayerChatBubble = isPlayerChatBubble,
				AllowTags = allowTags
			};

			SendTo(conn, msg);
			return msg;
		}
	}
}

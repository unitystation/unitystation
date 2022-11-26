using Messages.Client;
using Mirror;

namespace Messages.Server
{
	/// <summary>
	/// Message from server to client that indicate that other player is typing
	/// Sends only to player that's are nearby to speaker
	/// </summary>
	public class ServerTypingMessage : ServerMessage<ServerTypingMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public TypingState state;
			public uint targetID;
		}

		//This is needed so the message can be discovered in NetworkManagerExtensions
		public NetMessage IgnoreMe;

		public override void Process(NetMessage msg)
		{
			// other client try to find networked identity that's typing
			LoadNetworkObject(msg.targetID);
			if (!NetworkObject)
				return;

			// than we change it typing icon
			var player = NetworkObject.GetComponent<PlayerScript>();
			if (!player)
				return;

			var icon = player.ChatIcon;
			if (!icon)
				return;

			var showTyping = msg.state == TypingState.TYPING;

			// check if player is conscious before generating typing icon
			if (player.playerHealth == null)
			{
				icon.ToggleTypingIcon(showTyping);
				return;
			}
			bool isPlayerConscious = (player.playerHealth.ConsciousState == ConsciousState.CONSCIOUS ||
			                          player.playerHealth.ConsciousState == ConsciousState.BARELY_CONSCIOUS);
			if (isPlayerConscious)
			{
				icon.ToggleTypingIcon(showTyping);
			}
		}

		public static NetMessage Send(PlayerScript player, TypingState state)
		{
			var msg = new NetMessage()
			{
				state = state,
				targetID = player.netId
			};

			var playerPos = player.transform.position;
			SendToNearbyPlayers(playerPos, msg);
			return msg;
		}
	}
}

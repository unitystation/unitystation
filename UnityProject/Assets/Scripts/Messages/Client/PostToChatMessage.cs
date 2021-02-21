using System.Collections;
using Messages.Client;
using Mirror;
using UnityEngine;

/// <summary>
///     Attempts to send a chat message to the server
/// </summary>
public class PostToChatMessage: ClientMessage
{
	public struct PostToChatMessageNetMessage : NetworkMessage
	{
		public ChatChannel Channels;
		public string ChatMessageText;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public PostToChatMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as PostToChatMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			Chat.AddChatMsgToChat(SentByPlayer, newMsg.ChatMessageText, newMsg.Channels);
		}
	}

	//This is only used to send the chat input on the client to the server
	public static PostToChatMessageNetMessage Send(string message, ChatChannel channels)
	{
		PostToChatMessageNetMessage msg = new PostToChatMessageNetMessage
		{
			Channels = channels,
			ChatMessageText = message
		};
		new PostToChatMessage().Send(msg);

		return msg;
	}
}

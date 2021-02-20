using System.Collections;
using Messages.Client;
using UnityEngine;

/// <summary>
///     Attempts to send a chat message to the server
/// </summary>
public class PostToChatMessage: ClientMessage
{
	public class PostToChatMessageNetMessage : ActualMessage
	{
		public ChatChannel Channels;
		public string ChatMessageText;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as PostToChatMessageNetMessage;
		if(newMsg == null) return;

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

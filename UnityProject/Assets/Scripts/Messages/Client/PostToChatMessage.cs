using System.Collections;
using Messages.Client;
using UnityEngine;

/// <summary>
///     Attempts to send a chat message to the server
/// </summary>
public class PostToChatMessage : ClientMessage
{
	public ChatChannel Channels;
	public string ChatMessageText;

	public override void Process()
	{
		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			Chat.AddChatMsgToChat(SentByPlayer, ChatMessageText, Channels);
		}
	}

	//This is only used to send the chat input on the client to the server
	public static PostToChatMessage Send(string message, ChatChannel channels)
	{
		PostToChatMessage msg = new PostToChatMessage
		{
			Channels = channels,
			ChatMessageText = message
		};
		msg.Send();

		return msg;
	}
}

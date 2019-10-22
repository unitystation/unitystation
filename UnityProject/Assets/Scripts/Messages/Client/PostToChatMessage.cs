using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Attempts to send a chat message to the server
/// </summary>
public class PostToChatMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.PostToChatMessage;
	public ChatChannel Channels;
	public string ChatMessageText;

	public override IEnumerator Process()
	{
		if (SentByPlayer != ConnectedPlayer.Invalid)
		{
			if (ValidRequest(SentByPlayer)) {
				Chat.AddChatMsgToChat(SentByPlayer, ChatMessageText, Channels);
			}
		}
		yield return null;
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

	public bool ValidRequest(ConnectedPlayer player)
	{
		//Need to add system channel here so player can transmit system level events but not select it in the UI
		ChatChannel availableChannels = ChatChannel.System;
		if (player.Script == null)
		{
			availableChannels |= ChatChannel.OOC;
		}
		else
		{
			availableChannels |= player.Script.GetAvailableChannelsMask();
		}

		if ((availableChannels & Channels) == Channels)
		{
			return true;
		}
		return false;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Channels = (ChatChannel) reader.ReadUInt32();
		ChatMessageText = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteInt32((int) Channels);
		writer.WriteString(ChatMessageText);
	}
}
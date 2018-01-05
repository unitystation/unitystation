﻿using System.Collections;
using PlayGroup;
using UnityEngine;
using UnityEngine.Networking;

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
		yield return WaitFor(SentBy);

		GameObject player = NetworkObject;
		if (ValidRequest(player))
		{
			ChatModifier modifiers = player.GetComponent<PlayerScript>().GetCurrentChatModifiers();
			ChatEvent chatEvent = new ChatEvent(ChatMessageText, player.name, Channels, modifiers);
			ChatRelay.Instance.AddToChatLogServer(chatEvent);
		}
	}

	//We want ChatEvent to be created on the server, so we're only passing the individual variables
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

	public bool ValidRequest(GameObject player)
	{
		PlayerScript playerScript = player.GetComponent<PlayerScript>();
		//Need to add system channel here so player can transmit system level events but not select it in the UI
		ChatChannel availableChannels = playerScript.GetAvailableChannels() | ChatChannel.System;
		if ((availableChannels & Channels) == Channels)
		{
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		return string.Format("[PostToChatMessage SentBy={0} ChatMessageText={1} Channels={2} MessageType={3}]",
			SentBy, ChatMessageText, Channels, MessageType);
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
		writer.Write((int) Channels);
		writer.Write(ChatMessageText);
	}
}
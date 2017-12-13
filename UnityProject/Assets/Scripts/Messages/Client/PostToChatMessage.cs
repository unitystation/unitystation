using System.Collections;
using InputControl;
using UnityEngine;
using UnityEngine.Networking;
using System;
using PlayGroup;

/// <summary>
/// Attempts to send a chat message to the server
/// </summary>
public class PostToChatMessage : ClientMessage<PostToChatMessage>
{
	public ChatChannel Channels;
	public string ChatMessageText;

	public override IEnumerator Process()
	{
		yield return WaitFor(SentBy);

		GameObject player = NetworkObject;
		if(ValidRequest(player)) {
			ChatModifier modifiers = player.GetComponent<PlayerScript>().GetCurrentChatModifiers();
			ChatEvent chatEvent = new ChatEvent(ChatMessageText, player.name, Channels, modifiers); 
			ChatRelay.Instance.AddToChatLogServer(chatEvent);
		}
	}

	//We want ChatEvent to be created on the server, so we're only passing the individual variables
	public static PostToChatMessage Send(string message, ChatChannel channels)
	{
		var msg = new PostToChatMessage
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
		if((playerScript.GetAvailableChannels() & Channels) == Channels){
			return true;
		}
		return false;
	}

	public override string ToString()
	{
		return string.Format("[PostToChatMessage SentBy={0} ChatMessageText={1} Channels={2}]",
			SentBy, ChatMessageText, Channels);
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Channels = (ChatChannel)reader.ReadUInt32();
		ChatMessageText = reader.ReadString();

	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write((Int32)Channels);
		writer.Write(ChatMessageText);
	}
}

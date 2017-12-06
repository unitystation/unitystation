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

		GameObject player = NetworkObjects[0];
		if(Validate(player)) {
			ChatModifier modifiers = player.GetComponent<PlayerScript>().GetCurrentChatModifiers();
			ChatEvent chatEvent = new ChatEvent(ChatMessageText, player.name, Channels, modifiers);

			ChatRelay.Instance.AddToChatLog(chatEvent);
		}
	}

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

	public bool Validate(GameObject player)
	{
		PlayerScript playerScript = player.GetComponent<PlayerScript>();

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
		Channels = (ChatChannel)reader.ReadInt16();
		ChatMessageText = reader.ReadString();

	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write((Int16)Channels);
		writer.Write(ChatMessageText);
	}
}

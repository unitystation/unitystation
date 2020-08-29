using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
/// Message that tells client to add a ChatEvent to their chat.
/// NetMsg is being used so each recipient can be targeted individually.
/// This allows for easy direct client updating (i.e. for whispering)
/// and ensures snooping can not happen on communications
/// </summary>
public class UpdateChatMessage : ServerMessage
{
	public ChatChannel Channels;
	public ChatModifier ChatModifiers;
	public string Message;
	//If OthersMessage is empty then Message is meant for everyone, otherwise Message is meant for originator
	// and others is meant for everyone else
	public string OthersMessage;
	public uint Recipient;
	public uint Originator;
	public string Speaker;

	public override void Process()
	{
		LoadNetworkObject(Recipient);
		Chat.ProcessUpdateChatMessage(Recipient, Originator, Message, OthersMessage, Channels, ChatModifiers, Speaker);
	}

	/// <summary>
	/// Do not use this message directly. If you need to do work with the chat system use
	/// the Chat API (the only exception to this rule is if you just need to send 1 msg to 1 client from the server
	/// i.e syndi special roles)
	/// </summary>
	public static UpdateChatMessage Send(GameObject recipient, ChatChannel channels, ChatModifier chatMods, string chatMessage, string othersMsg = "",
		GameObject originator = null, string speaker = "")
	{
		uint origin = NetId.Empty;
		if (originator != null)
		{
			origin = originator.GetComponent<NetworkIdentity>().netId;
		}

		UpdateChatMessage msg =
			new UpdateChatMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Channels = channels,
				ChatModifiers = chatMods,
				Message = chatMessage,
				OthersMessage = othersMsg,
				Originator = origin,
				Speaker = speaker
			};

		msg.SendTo(recipient);
		return msg;
	}
}

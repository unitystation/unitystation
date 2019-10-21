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
	public static short MessageType = (short) MessageTypes.UpdateChatMessage;
	public ChatChannel Channels;
	public string Message;
	//If OthersMessage is empty then Message is meant for everyone, otherwise Message is meant for originator
	// and others is meant for everyone else
	public string OthersMessage;
	public uint Recipient;
	public uint Originator;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		Chat.ProcessUpdateChatMessage(Recipient, Originator, Message, OthersMessage, Channels);
	}

	public static UpdateChatMessage Send(GameObject recipient, ChatChannel channels, string chatMessage, string othersMsg = "", GameObject originator = null)
	{
		uint origin = NetId.Empty;
		if (originator != null)
		{
			origin = recipient.GetComponent<NetworkIdentity>().netId;
		}

		UpdateChatMessage msg =
			new UpdateChatMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Channels = channels,
				Message = chatMessage,
				OthersMessage = othersMsg,
				Originator = origin
			};

		msg.SendTo(recipient);
		return msg;
	}
}
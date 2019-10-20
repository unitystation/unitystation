using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class UpdateChatMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateChatMessage;
	public ChatChannel Channels;
	public string ChatMessage;
	//For Action/Combat based messages an Originator chat message is given also
	//(i.e. 'You hugged ian' instead of 'Cuban Pete hugged ian')
	public string OriginatorChatMessage;
	public uint Recipient;
	public uint Originator;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);

		ChatRelay.Instance.AddToChatLogClient(ChatMessage, Channels);
	}

	public static UpdateChatMessage Send(GameObject recipient, ChatChannel channels, string chatMessage, string originatorMsg = "", GameObject originator = null)
	{
		uint origin = NetId.Empty;
		if (originator != null)
		{
			origin = recipient.GetComponent<NetworkIdentity>().netId;
		}

		UpdateChatMessage msg =
			new UpdateChatMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				Channels = channels,
				ChatMessage = chatMessage,
				OriginatorChatMessage = originatorMsg,
				Originator = origin
			};

		msg.SendTo(recipient);
		return msg;
	}
}
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using PlayGroup;

/// <summary>
/// Message that tells client to add a ChatEvent to their chat 
/// </summary>
public class UpdateChatMessage : ServerMessage<UpdateChatMessage>
{
    public NetworkInstanceId Recipient;
	public ChatChannel Channels;
	public ChatModifier Modifiers;
	public string ChatMessageText;
	public string ChatMessageSender;

	public override IEnumerator Process()
    {
        yield return WaitFor(Recipient);

		ChatRelay.Instance.AddToChatLogClient(ChatMessageText, ChatMessageSender, Channels, Modifiers);
	}

    public static UpdateChatMessage Send(GameObject recipient, ChatChannel channels, ChatModifier modifiers, string message, string sender)
    {
		var msg = new UpdateChatMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			Channels = channels,
			Modifiers = modifiers,
			ChatMessageText = message,
			ChatMessageSender = sender
		};

        msg.SendTo(recipient);
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[UpdateChatMessage Recipient={0} Channels={1} Modifiers={2} ChatMessageText={3} ChatMessageSender{4}]",
                                                        Recipient, Channels, Modifiers, ChatMessageText, ChatMessageSender);
    }
}

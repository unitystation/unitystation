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
	public string ChatMessageText;

	public override IEnumerator Process()
    {
        yield return WaitFor(Recipient);

		ChatRelay.Instance.AddToChatLogClient(ChatMessageText, Channels);
	}

    public static UpdateChatMessage Send(GameObject recipient, ChatChannel channels, string message)
    {
		var msg = new UpdateChatMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
			Channels = channels,
			ChatMessageText = message,
		};

        msg.SendTo(recipient);
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[UpdateChatMessage Recipient={0} Channels={1} ChatMessageText={2}]",
                                                        Recipient, Channels, ChatMessageText);
    }
}

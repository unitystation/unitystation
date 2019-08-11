using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Message that tells client to add a ChatEvent to their chat
/// </summary>
public class UpdateChatMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateChatMessage;
	public ChatChannel Channels;
	public string ChatMessageText;
	public NetworkInstanceId Recipient;
	//A list of NetId values of players who are in the visible local area
	//in relation to the recipient
	public string localGroupJson;
	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);

		ChatRelay.Instance.AddToChatLogClient(ChatMessageText, Channels);
	}

	public static UpdateChatMessage Send(GameObject recipient, ChatChannel channels, string message, NetIDGroup localGroup = null)
	{
		//Convert any localGroupData to json
		var jsonData = "";
		if(localGroup != null) jsonData = JsonUtility.ToJson(localGroup);

		UpdateChatMessage msg =
			new UpdateChatMessage {Recipient = recipient.GetComponent<NetworkIdentity>().netId, Channels = channels, ChatMessageText = message, localGroupJson = jsonData};

		msg.SendTo(recipient);
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[UpdateChatMessage Recipient={0} Channels={1} ChatMessageText={2}]", Recipient, Channels, ChatMessageText);
	}
}
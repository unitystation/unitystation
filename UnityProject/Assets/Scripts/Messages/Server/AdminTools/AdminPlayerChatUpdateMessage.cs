using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminPlayerChatUpdateMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.AdminPlayerChatUpdateMessage;
	public string JsonData;
	public string PlayerId;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();

		UIManager.Instance.adminChatWindows.adminPlayerChat.ClientUpdateChatLog(JsonData, PlayerId);
	}

	public static AdminPlayerChatUpdateMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage, string playerId)
	{
		AdminChatUpdate update = new AdminChatUpdate();
		update.messages.Add(chatMessage);
		AdminPlayerChatUpdateMessage  msg =
			new AdminPlayerChatUpdateMessage  {JsonData = JsonUtility.ToJson(update), PlayerId = playerId};

		msg.SendToAdmins();
		return msg;
	}

	public static AdminPlayerChatUpdateMessage SendLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update, string playerId)
	{
		AdminPlayerChatUpdateMessage msg =
			new AdminPlayerChatUpdateMessage
			{
				JsonData = JsonUtility.ToJson(update),
				PlayerId = playerId
			};

		msg.SendTo(requestee);
		return msg;
	}
}

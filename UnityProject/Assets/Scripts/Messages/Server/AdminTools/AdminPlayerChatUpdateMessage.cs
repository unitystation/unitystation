using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminPlayerChatUpdateMessage : ServerMessage
{
	public string JsonData;
	public string PlayerId;

	public override void Process()
	{
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

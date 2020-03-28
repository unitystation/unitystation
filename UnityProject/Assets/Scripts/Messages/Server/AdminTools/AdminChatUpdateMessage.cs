using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminChatUpdateMessage : ServerMessage
{
	public override short MessageType => (short) MessageTypes.AdminChatUpdateMessage;
	public string JsonData;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();

		UIManager.Instance.adminChatWindows.adminToAdminChat.ClientUpdateChatLog(JsonData);
	}

	public static AdminChatUpdateMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage)
	{
		AdminChatUpdate update = new AdminChatUpdate();
		update.messages.Add(chatMessage);
		AdminChatUpdateMessage  msg =
			new AdminChatUpdateMessage  {JsonData = JsonUtility.ToJson(update) };

		msg.SendToAdmins();
		return msg;
	}

	public static AdminChatUpdateMessage SendLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update)
	{
		AdminChatUpdateMessage msg =
			new AdminChatUpdateMessage
			{
				JsonData = JsonUtility.ToJson(update),
			};

		msg.SendTo(requestee);
		return msg;
	}
}

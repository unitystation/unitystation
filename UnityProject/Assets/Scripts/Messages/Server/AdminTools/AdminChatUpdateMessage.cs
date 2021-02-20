using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminChatUpdateMessage : ServerMessage
{
	public class AdminChatUpdateMessageNetMessage : ActualMessage
	{
		public string JsonData;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AdminChatUpdateMessageNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.adminChatWindows.adminToAdminChat.ClientUpdateChatLog(newMsg.JsonData);
	}

	public static AdminChatUpdateMessageNetMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage)
	{
		AdminChatUpdate update = new AdminChatUpdate();
		update.messages.Add(chatMessage);
		AdminChatUpdateMessageNetMessage  msg =
			new AdminChatUpdateMessageNetMessage  {JsonData = JsonUtility.ToJson(update) };

		new AdminChatUpdateMessage().SendToAdmins(msg);
		return msg;
	}

	public static AdminChatUpdateMessageNetMessage SendLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update)
	{
		AdminChatUpdateMessageNetMessage msg =
			new AdminChatUpdateMessageNetMessage
			{
				JsonData = JsonUtility.ToJson(update),
			};

		new AdminChatUpdateMessage().SendTo(requestee, msg);
		return msg;
	}
}

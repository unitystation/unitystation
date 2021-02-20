using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminPlayerChatUpdateMessage : ServerMessage
{
	public class AdminPlayerChatUpdateMessageNetMessage : ActualMessage
	{
		public string JsonData;
		public string PlayerId;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AdminPlayerChatUpdateMessageNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.adminChatWindows.adminPlayerChat.ClientUpdateChatLog(newMsg.JsonData, newMsg.PlayerId);
	}

	public static AdminPlayerChatUpdateMessageNetMessage SendSingleEntryToAdmins(AdminChatMessage chatMessage, string playerId)
	{
		AdminChatUpdate update = new AdminChatUpdate();
		update.messages.Add(chatMessage);
		AdminPlayerChatUpdateMessageNetMessage  msg =
			new AdminPlayerChatUpdateMessageNetMessage  {JsonData = JsonUtility.ToJson(update), PlayerId = playerId};

		new AdminPlayerChatUpdateMessage().SendToAdmins(msg);
		return msg;
	}

	public static AdminPlayerChatUpdateMessageNetMessage SendLogUpdateToAdmin(NetworkConnection requestee, AdminChatUpdate update, string playerId)
	{
		AdminPlayerChatUpdateMessageNetMessage msg =
			new AdminPlayerChatUpdateMessageNetMessage
			{
				JsonData = JsonUtility.ToJson(update),
				PlayerId = playerId
			};

		new AdminPlayerChatUpdateMessage().SendTo(requestee, msg);
		return msg;
	}
}

using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class AdminPlayerChatUpdateMessage : ServerMessage
{
	public struct AdminPlayerChatUpdateMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public string PlayerId;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminPlayerChatUpdateMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminPlayerChatUpdateMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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

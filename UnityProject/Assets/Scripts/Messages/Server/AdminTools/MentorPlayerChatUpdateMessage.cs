using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class MentorPlayerChatUpdateMessage : ServerMessage
{
	public class MentorPlayerChatUpdateMessageNetMessage : NetworkMessage
	{
		public string JsonData;
		public string PlayerId;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as MentorPlayerChatUpdateMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		UIManager.Instance.adminChatWindows.mentorPlayerChat.ClientUpdateChatLog(newMsg.JsonData, newMsg.PlayerId);
	}

	public static MentorPlayerChatUpdateMessageNetMessage SendSingleEntryToMentors(AdminChatMessage chatMessage, string playerId)
	{
		AdminChatUpdate update = new AdminChatUpdate();
		update.messages.Add(chatMessage);

		MentorPlayerChatUpdateMessageNetMessage  msg = new MentorPlayerChatUpdateMessageNetMessage
		{
			JsonData = JsonUtility.ToJson(update),
			PlayerId = playerId
		};

		new MentorPlayerChatUpdateMessage().SendToMentors(msg);
		return msg;
	}

	public static MentorPlayerChatUpdateMessageNetMessage SendLogUpdateToMentor(NetworkConnection requestee, AdminChatUpdate update, string playerId)
	{
		MentorPlayerChatUpdateMessageNetMessage msg = new MentorPlayerChatUpdateMessageNetMessage
		{
			JsonData = JsonUtility.ToJson(update),
			PlayerId = playerId
		};

		new MentorPlayerChatUpdateMessage().SendTo(requestee, msg);
		return msg;
	}
}

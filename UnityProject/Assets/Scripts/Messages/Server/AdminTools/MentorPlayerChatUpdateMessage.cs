using System.Collections;
using UnityEngine;
using Mirror;
using AdminTools;

public class MentorPlayerChatUpdateMessage : ServerMessage
{
	public string JsonData;
	public string PlayerId;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.mentorPlayerChat.ClientUpdateChatLog(JsonData, PlayerId);
	}

	public static MentorPlayerChatUpdateMessage SendSingleEntryToMentors(AdminChatMessage chatMessage, string playerId)
	{
		AdminChatUpdate update = new AdminChatUpdate();
		update.messages.Add(chatMessage);
		MentorPlayerChatUpdateMessage  msg =
			new MentorPlayerChatUpdateMessage  {JsonData = JsonUtility.ToJson(update), PlayerId = playerId};

		msg.SendToMentors();
		return msg;
	}

	public static MentorPlayerChatUpdateMessage SendLogUpdateToMentor(NetworkConnection requestee, AdminChatUpdate update, string playerId)
	{
		MentorPlayerChatUpdateMessage msg =
			new MentorPlayerChatUpdateMessage
			{
				JsonData = JsonUtility.ToJson(update),
				PlayerId = playerId
			};

		msg.SendTo(requestee);
		return msg;
	}
}

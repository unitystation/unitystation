using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class RequestMentorBwoink : ClientMessage
{
	public class RequestMentorBwoinkNetMessage : ActualMessage
	{
		public string Userid;
		public string MentorToken;
		public string UserToBwoink;
		public string Message;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as RequestMentorBwoinkNetMessage;
		if(newMsg == null) return;

		VerifyMentorStatus(newMsg);
	}

	void VerifyMentorStatus(RequestMentorBwoinkNetMessage msg)
	{
		var player = PlayerList.Instance.GetMentor(msg.Userid, msg.MentorToken);
		if (player == null)
		{
			player = PlayerList.Instance.GetAdmin(msg.Userid, msg.MentorToken);
			if(player == null){
				//theoretically this shouldnt happen, and indicates someone might be tampering with the client.
				return;
			}
		}
		var recipient = PlayerList.Instance.GetAllByUserID(msg.UserToBwoink);
		foreach (var r in recipient)
		{
			MentorBwoinkMessage.Send(r.GameObject, msg.Userid, "<color=#6400FF>" + msg.Message + "</color>");
			UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(msg.Message, msg.UserToBwoink, msg.Userid);
		}
	}

	public static RequestMentorBwoinkNetMessage Send(string userId, string mentorToken, string userIDToBwoink, string message)
	{
		RequestMentorBwoinkNetMessage msg = new RequestMentorBwoinkNetMessage
		{
			Userid = userId,
			MentorToken = mentorToken,
			UserToBwoink = userIDToBwoink,
			Message = message
		};
		new RequestMentorBwoink().Send(msg);
		return msg;
	}
}

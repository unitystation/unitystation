using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestMentorBwoink : ClientMessage
{
	public struct RequestMentorBwoinkNetMessage : NetworkMessage
	{
		public string Userid;
		public string MentorToken;
		public string UserToBwoink;
		public string Message;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestMentorBwoinkNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestMentorBwoinkNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

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

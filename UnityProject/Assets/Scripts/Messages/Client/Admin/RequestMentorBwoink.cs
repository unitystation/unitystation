﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Messages.Client;
using Mirror;

public class RequestMentorBwoink : ClientMessage
{
	public string Userid;
	public string MentorToken;
	public string UserToBwoink;
	public string Message;

	public override void Process()
	{
		VerifyMentorStatus();
	}

	void VerifyMentorStatus()
	{
		var player = PlayerList.Instance.GetMentor(Userid, MentorToken);
		if (player == null)
		{
			player = PlayerList.Instance.GetAdmin(Userid,MentorToken);
			if(player == null){
				//theoretically this shouldnt happen, and indicates someone might be tampering with the client.
				return;
			}
		}
		var recipient = PlayerList.Instance.GetAllByUserID(UserToBwoink);
		foreach (var r in recipient)
		{
			MentorBwoinkMessage.Send(r.GameObject, Userid, "<color=#6400FF>" + Message + "</color>");
			UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(Message, UserToBwoink, Userid);
		}
	}

	public static RequestMentorBwoink Send(string userId, string mentorToken, string userIDToBwoink, string message)
	{
		RequestMentorBwoink msg = new RequestMentorBwoink
		{
			Userid = userId,
			MentorToken = mentorToken,
			UserToBwoink = userIDToBwoink,
			Message = message
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		MentorToken = reader.ReadString();
		UserToBwoink = reader.ReadString();
		Message = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(MentorToken);
		writer.WriteString(UserToBwoink);
		writer.WriteString(Message);
	}
}

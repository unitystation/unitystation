using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class MentorReplyMessage : ClientMessage
{
	public string Message;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(Message, SentByPlayer.UserId);
	}

	public static MentorReplyMessage Send(string message)
	{
		MentorReplyMessage msg = new MentorReplyMessage
		{
			Message = message
		};
		msg.Send();
		return msg;
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class MentorReplyMessage : ClientMessage
{
	public class MentorReplyMessageNetMessage : NetworkMessage
	{
		public string Message;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as MentorReplyMessageNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.adminChatWindows.mentorPlayerChat.ServerAddChatRecord(newMsg.Message, SentByPlayer.UserId);
	}

	public static MentorReplyMessageNetMessage Send(string message)
	{
		MentorReplyMessageNetMessage msg = new MentorReplyMessageNetMessage
		{
			Message = message
		};

		new MentorReplyMessage().Send(msg);
		return msg;
	}
}

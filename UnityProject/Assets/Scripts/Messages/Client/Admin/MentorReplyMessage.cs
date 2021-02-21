using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class MentorReplyMessage : ClientMessage
{
	public struct MentorReplyMessageNetMessage : NetworkMessage
	{
		public string Message;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public MentorReplyMessageNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as MentorReplyMessageNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

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
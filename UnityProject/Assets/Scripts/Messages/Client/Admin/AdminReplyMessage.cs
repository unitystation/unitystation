using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class AdminReplyMessage : ClientMessage
{
	public string Message;

	public override void Process()
	{
		UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(Message, SentByPlayer.UserId);
	}

	public static AdminReplyMessage Send(string message)
	{
		AdminReplyMessage msg = new AdminReplyMessage
		{
			Message = message
		};
		msg.Send();
		return msg;
	}
}

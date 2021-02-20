using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class AdminReplyMessage : ClientMessage
{
	public class AdminReplyMessageNetMessage : ActualMessage
	{
		public string Message;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as AdminReplyMessageNetMessage;
		if(newMsg == null) return;

		UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(newMsg.Message, SentByPlayer.UserId);
	}

	public static AdminReplyMessageNetMessage Send(string message)
	{
		AdminReplyMessageNetMessage msg = new AdminReplyMessageNetMessage
		{
			Message = message
		};
		new AdminReplyMessage().Send(msg);
		return msg;
	}
}

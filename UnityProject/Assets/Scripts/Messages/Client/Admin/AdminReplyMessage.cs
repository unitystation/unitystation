using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminReplyMessage : ClientMessage
{
	public class AdminReplyMessageNetMessage : NetworkMessage
	{
		public string Message;
	}

	public override void Process<T>(T msg)
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

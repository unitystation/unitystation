using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class AdminReplyMessage : ClientMessage
{
	public struct AdminReplyMessageNetMessage : NetworkMessage
	{
		public string Message;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public AdminReplyMessageNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as AdminReplyMessageNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

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
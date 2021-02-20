using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestAdminChatMessage : ClientMessage
{
	public class RequestAdminChatMessageNetMessage : ActualMessage
	{
		public string Userid;
		public string AdminToken;
		public string Message;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as RequestAdminChatMessageNetMessage;
		if(newMsg == null) return;

		VerifyAdminStatus(newMsg);
	}

	void VerifyAdminStatus(RequestAdminChatMessageNetMessage msg)
	{
		var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (player != null)
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(msg.Message, msg.Userid);
		}
	}

	public static RequestAdminChatMessageNetMessage Send(string userId, string adminToken, string message)
	{
		RequestAdminChatMessageNetMessage msg = new RequestAdminChatMessageNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			Message = message
		};
		new RequestAdminChatMessage().Send(msg);
		return msg;
	}
}

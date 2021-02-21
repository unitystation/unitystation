using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestAdminChatMessage : ClientMessage
{
	public struct RequestAdminChatMessageNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public string Message;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestAdminChatMessageNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestAdminChatMessageNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestAdminChatMessage : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public string Message;

	public override void Process()
	{
		VerifyAdminStatus();
	}

	void VerifyAdminStatus()
	{
		var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (player != null)
		{
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(Message, Userid);
		}
	}

	public static RequestAdminChatMessage Send(string userId, string adminToken, string message)
	{
		RequestAdminChatMessage msg = new RequestAdminChatMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			Message = message
		};
		msg.Send();
		return msg;
	}
}

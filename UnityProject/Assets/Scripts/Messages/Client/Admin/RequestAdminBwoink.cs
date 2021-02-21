using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestAdminBwoink : ClientMessage
{
	public class RequestAdminBwoinkNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public string UserToBwoink;
		public string Message;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as RequestAdminBwoinkNetMessage;
		if(newMsg == null) return;

		VerifyAdminStatus(newMsg);
	}

	void VerifyAdminStatus(RequestAdminBwoinkNetMessage msg)
	{
		var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (player != null)
		{
			var recipient = PlayerList.Instance.GetAllByUserID(msg.UserToBwoink);
			foreach (var r in recipient)
			{
				AdminBwoinkMessage.Send(r.GameObject, msg.Userid, "<color=red>" + msg.Message + "</color>");
				UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(msg.Message, msg.UserToBwoink, msg.Userid);
			}
		}
	}

	public static RequestAdminBwoinkNetMessage Send(string userId, string adminToken, string userIDToBwoink, string message)
	{
		RequestAdminBwoinkNetMessage msg = new RequestAdminBwoinkNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToBwoink = userIDToBwoink,
			Message = message
		};
		new RequestAdminBwoink().Send(msg);
		return msg;
	}
}

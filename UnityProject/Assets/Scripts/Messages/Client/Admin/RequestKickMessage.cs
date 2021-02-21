using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestKickMessage : ClientMessage
{
	public struct RequestKickMessageNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public string UserToKick;
		public string Reason;
		public bool IsBan;
		public int BanMinutes;
		public bool AnnounceBan;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestKickMessageNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestKickMessageNetMessage?;
		if(newMsgNull == null) return;
		var newMsg = newMsgNull.Value;

		VerifyAdminStatus(newMsg);
	}

	void VerifyAdminStatus(RequestKickMessageNetMessage msg)
	{
		var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (player != null)
		{
			PlayerList.Instance.ProcessKickRequest(msg.Userid, msg.UserToKick, msg.Reason, msg.IsBan, msg.BanMinutes, msg.AnnounceBan);
		}
	}

	public static RequestKickMessageNetMessage Send(string userId, string adminToken, string userIDToKick, string reason,
		bool ban = false, int banminutes = 0, bool announceBan = true)
	{
		RequestKickMessageNetMessage msg = new RequestKickMessageNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToKick = userIDToKick,
			Reason = reason,
			IsBan = ban,
			BanMinutes = banminutes,
			AnnounceBan = announceBan
		};

		new RequestKickMessage().Send(msg);
		return msg;
	}
}

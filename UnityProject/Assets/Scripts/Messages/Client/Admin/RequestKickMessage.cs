using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class RequestKickMessage : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public string UserToKick;
	public string Reason;
	public bool IsBan;
	public int BanMinutes;
	public bool AnnounceBan;

	public override void Process()
	{
		VerifyAdminStatus();
	}

	void VerifyAdminStatus()
	{
		var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (player != null)
		{
			PlayerList.Instance.ProcessKickRequest(Userid, UserToKick, Reason, IsBan, BanMinutes, AnnounceBan);
		}
	}

	public static RequestKickMessage Send(string userId, string adminToken, string userIDToKick, string reason,
		bool ban = false, int banminutes = 0, bool announceBan = true)
	{
		RequestKickMessage msg = new RequestKickMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToKick = userIDToKick,
			Reason = reason,
			IsBan = ban,
			BanMinutes = banminutes,
			AnnounceBan = announceBan
		};
		msg.Send();
		return msg;
	}
}

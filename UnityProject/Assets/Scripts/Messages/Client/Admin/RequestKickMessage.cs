using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

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

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
		UserToKick = reader.ReadString();
		Reason = reader.ReadString();
		IsBan = reader.ReadBoolean();
		BanMinutes = reader.ReadInt32();
		AnnounceBan = reader.ReadBoolean();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToKick);
		writer.WriteString(Reason);
		writer.WriteBoolean(IsBan);
		writer.WriteInt32(BanMinutes);
		writer.WriteBoolean(AnnounceBan);
	}
}

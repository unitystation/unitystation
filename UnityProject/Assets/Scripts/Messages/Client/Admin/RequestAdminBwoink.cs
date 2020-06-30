using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class RequestAdminBwoink : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public string UserToBwoink;
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
			var recipient = PlayerList.Instance.GetAllByUserID(UserToBwoink);
			foreach (var r in recipient)
			{
				AdminBwoinkMessage.Send(r.GameObject, Userid, "<color=red>" + Message + "</color>");
				UIManager.Instance.adminChatWindows.adminPlayerChat.ServerAddChatRecord(Message, UserToBwoink, Userid);
			}
		}
	}

	public static RequestAdminBwoink Send(string userId, string adminToken, string userIDToBwoink, string message)
	{
		RequestAdminBwoink msg = new RequestAdminBwoink
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToBwoink = userIDToBwoink,
			Message = message
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
		UserToBwoink = reader.ReadString();
		Message = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToBwoink);
		writer.WriteString(Message);
	}
}

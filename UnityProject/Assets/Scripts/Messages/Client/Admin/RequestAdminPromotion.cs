using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AdminTools;
using Mirror;

public class RequestAdminPromotion : ClientMessage
{
	public override short MessageType => (short) MessageTypes.RequestEnableAdmin;

	public string Userid;
	public string AdminToken;
	public string UserToPromote;

	public override IEnumerator Process()
	{
		yield return new WaitForEndOfFrame();
		VerifyAdminStatus();
	}

	void VerifyAdminStatus()
	{
		var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (player != null)
		{
			PlayerList.Instance.ProcessAdminEnableRequest(Userid, UserToPromote);
		}
	}

	public static RequestAdminPromotion Send(string userId, string adminToken, string userIDToPromote)
	{
		RequestAdminPromotion msg = new RequestAdminPromotion
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToPromote= userIDToPromote,
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
		UserToPromote = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
		writer.WriteString(UserToPromote);
	}
}

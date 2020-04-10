using System.Collections;
using UnityEngine;
using Utility = UnityEngine.Networking.Utility;
using Mirror;

/// <summary>
///     Request admin page data from the server
/// </summary>
public class RequestAdminPageRefresh : ClientMessage
{
	public string Userid;
	public string AdminToken;

	public override void Process()
	{
		VerifyAdminStatus();
	}

	void VerifyAdminStatus()
	{
		var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (player != null)
		{
			AdminToolRefreshMessage.Send(player, Userid);
		}
	}

	public static RequestAdminPageRefresh Send(string userId, string adminToken)
	{
		RequestAdminPageRefresh msg = new RequestAdminPageRefresh
		{
			Userid = userId,
			AdminToken = adminToken
		};
		msg.Send();
		return msg;
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Userid = reader.ReadString();
		AdminToken = reader.ReadString();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.WriteString(Userid);
		writer.WriteString(AdminToken);
	}
}
using System.Collections;
using Messages.Client;
using UnityEngine;

/// <summary>
///     Request admin page data from the server
/// </summary>
public class RequestAdminPlayerList : ClientMessage
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
		if (player == null)
		{
			player = PlayerList.Instance.GetMentor(Userid,AdminToken);
			if(player == null)
				return;
		}
		AdminPlayerListRefreshMessage.Send(player, Userid);
	}

	public static RequestAdminPlayerList Send(string userId, string adminToken)
	{
		RequestAdminPlayerList msg = new RequestAdminPlayerList
		{
			Userid = userId,
			AdminToken = adminToken
		};
		msg.Send();
		return msg;
	}
}

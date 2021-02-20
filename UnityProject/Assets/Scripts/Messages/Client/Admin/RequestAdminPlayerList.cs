using System.Collections;
using Messages.Client;
using UnityEngine;

/// <summary>
///     Request admin page data from the server
/// </summary>
public class RequestAdminPlayerList : ClientMessage
{
	public class RequestAdminPlayerListNetMessage : ActualMessage
	{
		public string Userid;
		public string AdminToken;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as RequestAdminPlayerListNetMessage;
		if(newMsg == null) return;

		VerifyAdminStatus(newMsg);
	}

	void VerifyAdminStatus(RequestAdminPlayerListNetMessage msg)
	{
		var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (player == null)
		{
			player = PlayerList.Instance.GetMentor(msg.Userid, msg.AdminToken);
			if(player == null)
				return;
		}
		AdminPlayerListRefreshMessage.Send(player, msg.Userid);
	}

	public static RequestAdminPlayerListNetMessage Send(string userId, string adminToken)
	{
		RequestAdminPlayerListNetMessage msg = new RequestAdminPlayerListNetMessage
		{
			Userid = userId,
			AdminToken = adminToken
		};
		new RequestAdminPlayerList().Send(msg);
		return msg;
	}
}

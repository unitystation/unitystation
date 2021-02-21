using System.Collections;
using Messages.Client;
using Mirror;
using UnityEngine;

/// <summary>
///     Request admin page data from the server
/// </summary>
public class RequestAdminPageRefresh : ClientMessage
{
	public class RequestAdminPageRefreshNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
	}

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestAdminPageRefreshNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		VerifyAdminStatus(newMsg);
	}

	void VerifyAdminStatus(RequestAdminPageRefreshNetMessage msg)
	{
		var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (player != null)
		{
			AdminToolRefreshMessage.Send(player, msg.Userid);
		}
	}

	public static RequestAdminPageRefreshNetMessage Send(string userId, string adminToken)
	{
		RequestAdminPageRefreshNetMessage msg = new RequestAdminPageRefreshNetMessage
		{
			Userid = userId,
			AdminToken = adminToken
		};

		new RequestAdminPageRefresh().Send(msg);
		return msg;
	}
}

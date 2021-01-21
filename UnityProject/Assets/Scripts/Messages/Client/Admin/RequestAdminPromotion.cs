using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;

public class RequestAdminPromotion : ClientMessage
{
	public string Userid;
	public string AdminToken;
	public string UserToPromote;

	public override void Process()
	{
		VerifyAdminStatus();
	}

	void VerifyAdminStatus()
	{
		var player = PlayerList.Instance.GetAdmin(Userid, AdminToken);
		if (player != null)
		{
			PlayerList.Instance.ProcessAdminEnableRequest(Userid, UserToPromote);
			var user = PlayerList.Instance.GetByUserID(UserToPromote);
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{player.Player().Username} made {user.Name} an admin. Users ID is: {UserToPromote}", Userid);
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
}

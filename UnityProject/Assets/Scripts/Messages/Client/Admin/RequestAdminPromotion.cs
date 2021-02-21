using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Messages.Client;
using Mirror;

public class RequestAdminPromotion : ClientMessage
{
	public struct RequestAdminPromotionNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
		public string UserToPromote;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestAdminPromotionNetMessage message;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestAdminPromotionNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		VerifyAdminStatus(newMsg);
	}

	void VerifyAdminStatus(RequestAdminPromotionNetMessage msg)
	{
		var player = PlayerList.Instance.GetAdmin(msg.Userid, msg.AdminToken);
		if (player != null)
		{
			PlayerList.Instance.ProcessAdminEnableRequest(msg.Userid, msg.UserToPromote);
			var user = PlayerList.Instance.GetByUserID(msg.UserToPromote);
			UIManager.Instance.adminChatWindows.adminToAdminChat.ServerAddChatRecord(
				$"{player.Player().Username} made {user.Name} an admin. Users ID is: {msg.UserToPromote}", msg.Userid);
		}
	}

	public static RequestAdminPromotionNetMessage Send(string userId, string adminToken, string userIDToPromote)
	{
		RequestAdminPromotionNetMessage msg = new RequestAdminPromotionNetMessage
		{
			Userid = userId,
			AdminToken = adminToken,
			UserToPromote= userIDToPromote,
		};
		new RequestAdminPromotion().Send(msg);
		return msg;
	}
}

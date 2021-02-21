using System.Collections;
using Messages.Client;
using Mirror;
using UnityEngine;

/// <summary>
///     Request admin page data from the server
/// </summary>
public class RequestAdminPlayerList : ClientMessage
{
	public struct RequestAdminPlayerListNetMessage : NetworkMessage
	{
		public string Userid;
		public string AdminToken;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestAdminPlayerListNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestAdminPlayerListNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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

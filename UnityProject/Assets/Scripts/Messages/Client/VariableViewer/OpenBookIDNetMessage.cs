using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class OpenBookIDNetMessage : ClientMessage
{
	public ulong BookID;
	public string AdminId;
	public string AdminToken;

	public override void Process()
	{
		ValidateAdmin();
	}

	void ValidateAdmin()
	{
		var admin = PlayerList.Instance.GetAdmin(AdminId, AdminToken);
		if (admin == null) return;
		VariableViewer.RequestSendBook(BookID, SentByPlayer.GameObject);
	}


	public static OpenBookIDNetMessage Send(ulong BookID, string adminId, string adminToken)
	{
		OpenBookIDNetMessage msg = new OpenBookIDNetMessage();
		msg.BookID = BookID;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.Send();
		return msg;
	}
}

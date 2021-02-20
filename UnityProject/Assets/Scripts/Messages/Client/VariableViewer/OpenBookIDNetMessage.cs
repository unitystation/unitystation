using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class OpenBookIDNetMessage : ClientMessage
{
	public class OpenBookIDNetMessageNetMessage : ActualMessage
	{
		public ulong BookID;
		public string AdminId;
		public string AdminToken;
	}

	public override void Process(ActualMessage msg)
	{
		var newMsg = msg as OpenBookIDNetMessageNetMessage;
		if(newMsg == null) return;

		ValidateAdmin(newMsg);
	}

	void ValidateAdmin(OpenBookIDNetMessageNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
		if (admin == null) return;
		VariableViewer.RequestSendBook(msg.BookID, SentByPlayer.GameObject);
	}


	public static OpenBookIDNetMessageNetMessage Send(ulong BookID, string adminId, string adminToken)
	{
		OpenBookIDNetMessageNetMessage msg = new OpenBookIDNetMessageNetMessage();
		msg.BookID = BookID;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;

		new OpenBookIDNetMessage().Send(msg);
		return msg;
	}
}

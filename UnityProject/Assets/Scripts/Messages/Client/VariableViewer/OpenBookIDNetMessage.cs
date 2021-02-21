using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class OpenBookIDNetMessage : ClientMessage
{
	public struct OpenBookIDNetMessageNetMessage : NetworkMessage
	{
		public ulong BookID;
		public string AdminId;
		public string AdminToken;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public OpenBookIDNetMessageNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as OpenBookIDNetMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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

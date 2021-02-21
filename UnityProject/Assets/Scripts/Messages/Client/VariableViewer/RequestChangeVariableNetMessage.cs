using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestChangeVariableNetMessage : ClientMessage
{
	public class RequestChangeVariableNetMessageNetMessage : NetworkMessage
	{
		public string newValue;
		public ulong PageID;
		public bool IsNewBookshelf = false;
		public bool SendToClient = false;
		public string AdminId;
		public string AdminToken;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as RequestChangeVariableNetMessageNetMessage;
		if(newMsg == null) return;

		ValidateAdmin(newMsg);
	}

	void ValidateAdmin(RequestChangeVariableNetMessageNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
		if (admin == null) return;
		VariableViewer.RequestChangeVariable(msg.PageID, msg.newValue, msg.SendToClient, SentByPlayer.GameObject, msg.AdminId);

		Logger.Log($"Admin {admin.name} changed variable {msg.PageID} (in VV) with a new value of: {msg.newValue} ",
			Category.Admin);
	}


	public static RequestChangeVariableNetMessageNetMessage Send(ulong _PageID, string _newValue ,bool InSendToClient , string adminId, string adminToken)
	{
		RequestChangeVariableNetMessageNetMessage msg = new RequestChangeVariableNetMessageNetMessage();
		msg.PageID = _PageID;
		msg.newValue = _newValue;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.SendToClient = InSendToClient;

		new RequestChangeVariableNetMessage().Send(msg);
		return msg;
	}
}
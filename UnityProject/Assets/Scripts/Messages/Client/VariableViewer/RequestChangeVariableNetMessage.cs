using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class RequestChangeVariableNetMessage : ClientMessage
{
	public string newValue;
	public ulong PageID;
	public bool IsNewBookshelf = false;
	public bool SendToClient = false;
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
		VariableViewer.RequestChangeVariable(PageID, newValue,SendToClient, SentByPlayer.GameObject, AdminId);

		Logger.Log($"Admin {admin.name} changed variable {PageID} (in VV) with a new value of: {newValue} ",
			Category.Admin);
	}


	public static RequestChangeVariableNetMessage Send(ulong _PageID, string _newValue ,bool InSendToClient , string adminId, string adminToken)
	{
		RequestChangeVariableNetMessage msg = new RequestChangeVariableNetMessage();
		msg.PageID = _PageID;
		msg.newValue = _newValue;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.SendToClient = InSendToClient;
		msg.Send();
		return msg;
	}
}
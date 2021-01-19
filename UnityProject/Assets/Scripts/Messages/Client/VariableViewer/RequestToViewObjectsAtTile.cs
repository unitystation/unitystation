using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;

public class RequestToViewObjectsAtTile : ClientMessage
{
	public Vector3 Location;
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

		VariableViewer.ProcessTile(Location,SentByPlayer.GameObject);
	}

	public static RequestToViewObjectsAtTile Send(Vector3 _Location, string adminId, string adminToken)
	{
		RequestToViewObjectsAtTile msg = new RequestToViewObjectsAtTile();
		msg.Location = _Location;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;
		msg.Send();
		return msg;
	}
}

using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestToViewObjectsAtTile : ClientMessage
{
	public class RequestToViewObjectsAtTileNetMessage : NetworkMessage
	{
		public Vector3 Location;
		public string AdminId;
		public string AdminToken;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as RequestToViewObjectsAtTileNetMessage;
		if(newMsg == null) return;

		ValidateAdmin(newMsg);
	}

	void ValidateAdmin(RequestToViewObjectsAtTileNetMessage msg)
	{
		var admin = PlayerList.Instance.GetAdmin(msg.AdminId, msg.AdminToken);
		if (admin == null) return;

		VariableViewer.ProcessTile(msg.Location, SentByPlayer.GameObject);
	}

	public static RequestToViewObjectsAtTileNetMessage Send(Vector3 _Location, string adminId, string adminToken)
	{
		RequestToViewObjectsAtTileNetMessage msg = new RequestToViewObjectsAtTileNetMessage();
		msg.Location = _Location;
		msg.AdminId = adminId;
		msg.AdminToken = adminToken;

		new RequestToViewObjectsAtTile().Send(msg);
		return msg;
	}
}

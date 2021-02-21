using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;

public class RequestToViewObjectsAtTile : ClientMessage
{
	public struct RequestToViewObjectsAtTileNetMessage : NetworkMessage
	{
		public Vector3 Location;
		public string AdminId;
		public string AdminToken;
	}

	//This is needed so the message can be discovered in NetworkManagerExtensions
	public RequestToViewObjectsAtTileNetMessage IgnoreMe;

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as RequestToViewObjectsAtTileNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

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

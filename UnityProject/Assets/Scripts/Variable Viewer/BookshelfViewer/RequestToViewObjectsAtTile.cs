using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RequestToViewObjectsAtTile : ClientMessage
{
	public static short MessageType = (short)MessageTypes.RequestToViewObjectsAtTile;
	public Vector3 Location;

	public override IEnumerator Process()
	{
		VariableViewer.ProcessTile(Location);
		yield return null;
	}


	public static RequestToViewObjectsAtTile Send(Vector3 _Location)
	{
		RequestToViewObjectsAtTile msg = new RequestToViewObjectsAtTile();
		msg.Location = _Location;
		msg.Send();
		return msg;
	}
}
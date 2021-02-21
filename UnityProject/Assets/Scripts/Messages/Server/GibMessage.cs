using System.Collections;
using Mirror;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public class GibMessageNetMessage : NetworkMessage { }

	public override void Process<T>(T msg)
	{
		var newMsgNull = msg as GibMessageNetMessage?;
		if(newMsgNull == null) return; var newMsg = newMsgNull.Value;

		foreach (LivingHealthBehaviour living in Object.FindObjectsOfType<LivingHealthBehaviour>())
		{
			living.Death();
		}
	}

	public static GibMessageNetMessage Send()
	{
		GibMessageNetMessage msg = new GibMessageNetMessage();
		new GibMessage().SendToAll(msg);
		return msg;
	}
}
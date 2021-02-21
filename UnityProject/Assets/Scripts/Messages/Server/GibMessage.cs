using System.Collections;
using Mirror;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public class GibMessageNetMessage : NetworkMessage { }

	public override void Process<T>(T msg)
	{
		var newMsg = msg as GibMessageNetMessage;
		if(newMsg == null) return;

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
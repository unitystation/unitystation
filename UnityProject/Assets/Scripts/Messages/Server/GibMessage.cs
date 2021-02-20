using System.Collections;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public class GibMessageNetMessage : ActualMessage { }

	public override void Process(ActualMessage msg)
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
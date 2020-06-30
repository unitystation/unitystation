using System.Collections;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public override void Process()
	{
		foreach (LivingHealthBehaviour living in Object.FindObjectsOfType<LivingHealthBehaviour>())
		{
			living.Death();
		}
	}

	public static GibMessage Send()
	{
		GibMessage msg = new GibMessage();
		msg.SendToAll();
		return msg;
	}
}
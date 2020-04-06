using System.Collections;
using UnityEngine;

public class GibMessage : ServerMessage
{
	public override IEnumerator Process()
	{
		foreach (LivingHealthBehaviour living in Object.FindObjectsOfType<LivingHealthBehaviour>())
		{
			living.Death();
		}

		yield return null;
	}

	public static GibMessage Send()
	{
		GibMessage msg = new GibMessage();
		msg.SendToAll();
		return msg;
	}
}
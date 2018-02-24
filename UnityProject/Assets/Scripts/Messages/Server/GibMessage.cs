using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GibMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.GibMessage;

	public override IEnumerator Process()
	{
		Debug.Log(ToString());

		foreach (HealthBehaviour living in Object.FindObjectsOfType<HealthBehaviour>())
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

	public override string ToString()
	{
		return $"[GibMessage Type={MessageType}]";
	}
}
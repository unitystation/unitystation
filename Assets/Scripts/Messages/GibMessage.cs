using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GibMessage : ServerMessage<GibMessage>
{
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
		Debug.Log("GibMessage");

		yield return WaitFor(Subject);

		foreach (var living in Object.FindObjectsOfType<Living>()) {
			living.lastDamager = "God";
			living.Death(true);
		}
	}

	public static GibMessage Send()
	{
		var msg = new GibMessage();
		msg.SendToAll();
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[InteractMessage Subject={0}]", Subject);
	}
}

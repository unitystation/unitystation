using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InteractMessage : GameMessage<InteractMessage> {
	public NetworkInstanceId Subject;

	public InteractMessage() {}

	public override IEnumerator Process()
	{
		Debug.Log("InteractMessage");

		yield return WaitFor(Subject);

		NetworkObject.GetComponent<InputControl.InputTrigger>().Interact();
	}

	public static InteractMessage Send(GameObject subject) 
	{
		var msg = new InteractMessage{ Subject = subject.GetComponent<NetworkIdentity>().netId };
		msg.Send();
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[InteractMessage Subject={0}]", Subject);
	}
}

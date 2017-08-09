using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InteractMessage : ClientMessage<InteractMessage>
{
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
		Debug.Log(ToString());

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
		return string.Format("[InteractMessage Subject={0} Type={1}]", Subject, MessageType);
	}
}

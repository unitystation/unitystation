using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InteractMessage : ClientMessage<InteractMessage>
{
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
		Debug.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);

		NetworkObjects[0].GetComponent<InputControl.InputTrigger>().From(NetworkObjects[1]).Interact();
	}

	public static InteractMessage Send(GameObject subject)
	{
		var msg = new InteractMessage{ Subject = subject.GetComponent<NetworkIdentity>().netId };
		msg.Send();
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[InteractMessage Subject={0} Type={1} SentBy={2}]", Subject, MessageType, SentBy);
	}
	
	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Subject = reader.ReadNetworkId();
	}	
	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Subject);
	}
	
}

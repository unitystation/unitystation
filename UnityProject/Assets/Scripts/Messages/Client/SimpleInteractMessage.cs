using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class SimpleInteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.SimpleInteractMessage;
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
		//		Debug.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);

		Debug.Log("SimpleInteractMessage: doing nothing");
		//		NetworkObjects[0].GetComponent<InputControl.InputTrigger>().From(NetworkObjects[1]).Interact();
	}

	public static SimpleInteractMessage Send(GameObject subject)
	{
		SimpleInteractMessage msg = new SimpleInteractMessage {Subject = subject.GetComponent<NetworkIdentity>().netId};
		msg.Send();
		return msg;
	}

	public override string ToString()
	{
		return string.Format("[SimpleInteractMessage Subject={0} Type={1} SentBy={2}]", Subject, MessageType, SentBy);
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
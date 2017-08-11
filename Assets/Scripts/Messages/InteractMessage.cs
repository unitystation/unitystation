using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InteractMessage : ClientMessage<InteractMessage>
{
	public string Hand;
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
		Debug.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);

		NetworkObjects[0].GetComponent<InputControl.InputTrigger>().From(NetworkObjects[1]).With(Hand).Interact();
	}

	public static InteractMessage Send(GameObject subject, string hand)
	{
		var msg = new InteractMessage
		{
			Subject = subject.GetComponent<NetworkIdentity>().netId,
			Hand = hand
		};
		msg.Send();
		return msg;
	}

//	private static byte detectHandPlaceholder(string handEventString)
//	{
//		//we better start using enums for that soon!
//		if ( handEventString.Equals("leftHand") )	return 1;
//		if ( handEventString.Equals("rightHand") )	return 2;
//		return 0;
//	}

	public override string ToString()
	{
		return string.Format("[InteractMessage Subject={0} Hand={3} Type={1} SentBy={2}]",
			Subject, MessageType, SentBy, Hand);
	}
	
	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Hand = reader.ReadString();
		Subject = reader.ReadNetworkId();

	}	
	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Hand);
		writer.Write(Subject);
	}
	
}

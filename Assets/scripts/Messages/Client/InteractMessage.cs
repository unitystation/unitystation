using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
/// <summary>
/// Informs server of interaction
/// </summary>
public class InteractMessage : ClientMessage<InteractMessage>
{
	public byte Hand;
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
//		Debug.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);

		NetworkObjects[0].GetComponent<InputControl.InputTrigger>().Interact(NetworkObjects[1], decodeHand(Hand));
	}

	public static InteractMessage Send(GameObject subject, string hand)
	{
		var msg = new InteractMessage
		{
			Subject = subject.GetComponent<NetworkIdentity>().netId,
			Hand = encodeHand(hand)
		};
		msg.Send();
		return msg;
	}

	private static byte encodeHand(string handEventString)
	{
		switch ( handEventString )
		{
			case "leftHand": return 1;
			case "rightHand": return 2;
		}
		return 0;
	}
	private static string decodeHand(byte handEventByte)
	{
		//we better start using enums for that soon!
		switch ( handEventByte )
		{
				case 1: return "leftHand";
				case 2: return "rightHand";
				default: return null;
		}
	}

	public override string ToString()
	{
		return string.Format("[InteractMessage Subject={0} Hand={3} Type={1} SentBy={2}]",
			Subject, MessageType, SentBy, decodeHand(Hand));
	}
	
	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Hand = reader.ReadByte();
		Subject = reader.ReadNetworkId();

	}	
	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Hand);
		writer.Write(Subject);
	}
	
}

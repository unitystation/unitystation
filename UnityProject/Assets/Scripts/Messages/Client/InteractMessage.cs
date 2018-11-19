using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Informs server of interaction
/// </summary>
public class InteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.InteractMessage;
	public byte Hand;
	public Vector3 Position;
	public NetworkInstanceId Subject;
	public bool UITrigger;

	public override IEnumerator Process()
	{
		//		Logger.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);
		if (!UITrigger)
		{
			NetworkObjects[0].GetComponent<InputTrigger>().Interact(NetworkObjects[1], Position, decodeHand(Hand));
		}
		else
		{
			NetworkObjects[0].GetComponent<InputTrigger>().UI_Interact(NetworkObjects[1], decodeHand(Hand));
		}
	}

	/// <summary>
	/// Send the object being interacted with and the hand variable
	/// </summary>
	public static InteractMessage Send(GameObject subject, string hand)
	{
		return Send(subject, subject.transform.position, hand);
	}

	public static InteractMessage Send(GameObject subject, string hand, bool UITrigger)
	{
		InteractMessage msg = new InteractMessage
		{
			Subject = subject.GetComponent<NetworkIdentity>().netId,
				Position = subject.transform.position,
				Hand = encodeHand(hand),
				UITrigger = true
		};
		msg.Send();
		//		InputTrigger.msgCache[msg.Subject] = Time.time;
		return msg;
	}

	public static InteractMessage Send(GameObject subject, Vector3 position, string hand)
	{
		InteractMessage msg = new InteractMessage
		{
			Subject = subject.GetComponent<NetworkIdentity>().netId,
				Position = position,
				Hand = encodeHand(hand)
		};
		msg.Send();
		//		InputTrigger.msgCache[msg.Subject] = Time.time;
		return msg;
	}

	private static byte encodeHand(string handEventString)
	{
		switch (handEventString)
		{
			case "leftHand":
				return 1;
			case "rightHand":
				return 2;
			default:
				return 0;
		}
	}

	private static string decodeHand(byte handEventByte)
	{
		//we better start using enums for that soon!
		switch (handEventByte)
		{
			case 1:
				return "leftHand";
			case 2:
				return "rightHand";
			default:
				return null;
		}
	}

	public override string ToString()
	{
		return $"[InteractMessage Subject={Subject} Hand={decodeHand( Hand )} Type={MessageType} SentBy={SentBy}]";
	}

	public override void Deserialize(NetworkReader reader)
	{
		base.Deserialize(reader);
		Hand = reader.ReadByte();
		Position = reader.ReadVector3();
		Subject = reader.ReadNetworkId();
		UITrigger = reader.ReadBoolean();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Hand);
		writer.Write(Position);
		writer.Write(Subject);
		writer.Write(UITrigger);
	}
}
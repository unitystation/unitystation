using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Informs server of UI Item Interaction
/// </summary>
public class UIInteractMessage : ClientMessage
{
	public static short MessageType = (short) MessageTypes.UIInteractMessage;
	public byte Hand;
	public NetworkInstanceId Subject;

	public override IEnumerator Process()
	{
//		Logger.Log("Processed " + ToString());

		yield return WaitFor(Subject, SentBy);

		NetworkObjects[0].GetComponent<InputTrigger>().UI_Interact(NetworkObjects[1], decodeHand(Hand));
	}

	public static UIInteractMessage Send(GameObject subject, string hand)
	{
		UIInteractMessage msg = new UIInteractMessage
		{
			Subject = subject.GetComponent<NetworkIdentity>().netId,
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
		Subject = reader.ReadNetworkId();
	}

	public override void Serialize(NetworkWriter writer)
	{
		base.Serialize(writer);
		writer.Write(Hand);
		writer.Write(Subject);
	}
}
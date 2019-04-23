using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Informs server of interaction. Implied that the sender of the message is the player performing the interaciton.
/// </summary>
public class InteractMessage : ClientMessage
{

	public static short MessageType = (short) MessageTypes.InteractMessage;
	/// <summary>
	/// Which hand is performing the interaction
	/// </summary>
	public byte Hand;
	/// <summary>
	/// Position being targeted by interaction
	/// </summary>
	public Vector3 Position;
	/// <summary>
	/// Varies. Sometimes subject is the netid of the object being used to interact at the
	/// specified position. Other times subject is the object being targeted by interaction. Regardless,
	/// Subject will always be the game object whose InputTrigger is invoked when this message is processed.
	/// </summary>
	public NetworkInstanceId Subject;
	//if true, indicates this interaction is happening in the subject's UI
	public bool UITrigger;

	public override IEnumerator Process()
	{
		//		Logger.Log("Processed " + ToString());

		yield return WaitFor(Subject);
		if (!UITrigger)
		{
			NetworkObject.GetComponent<InputTrigger>().Interact(SentByPlayer.GameObject, Position, decodeHand(Hand));
		}
		else
		{
			NetworkObject.GetComponent<InputTrigger>().UI_Interact(SentByPlayer.GameObject, decodeHand(Hand));
		}
	}

	/// <summary>
	/// Request an interaction on the specified subject via the specified hand
	/// </summary>
	/// <param name="subject">thing whose inputtrigger should be invoked</param>
	/// <param name="hand">hand performing the interaction</param>
	public static InteractMessage Send(GameObject subject, string hand)
	{
		return Send(subject, subject.transform.position, hand);
	}

	/// <summary>
	/// Request an interaction on the specified subject via the specified hand
	/// </summary>
	/// <param name="subject">thing whose inputtrigger should be invoked</param>
	/// <param name="hand">hand performing the interaction</param>
	/// <param name="UITrigger">true if this is a UI interaction, false if an interaction on the map</param>
	/// <returns></returns>
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

	/// <summary>
	/// Request an interaction on the specified position using the specified subject
	/// </summary>
	/// <param name="subject">thing whose inputtrigger should be invoked</param>
	/// <param name="position">position targeted by the interaction</param>
	/// <param name="hand">hand performing the interaction</param>
	/// <returns></returns>
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
		return $"[InteractMessage Subject={Subject} Hand={decodeHand( Hand )} Type={MessageType} SentBy={SentByPlayer}]";
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
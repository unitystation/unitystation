using System.Collections;
using InputControl;
using PlayGroups.Input;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Informs server of interaction
/// </summary>
public class InteractMessage : ClientMessage<InteractMessage>
{
    public byte Hand;
    public Vector3 Position;
    public NetworkInstanceId Subject;



    public override IEnumerator Process()
    {
        //		Debug.Log("Processed " + ToString());

        yield return WaitFor(Subject, SentBy);

        NetworkObjects[0].GetComponent<InputTrigger>().Interact(NetworkObjects[1], Position, decodeHand(Hand));
    }

    public static InteractMessage Send(GameObject subject, string hand)
    {
        return Send(subject, subject.transform.position, hand);
    }

    public static InteractMessage Send(GameObject subject, Vector3 position, string hand)
    {
        var msg = new InteractMessage
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
            case "leftHand": return 1;
            case "rightHand": return 2;
            default: return 0;
        }
    }

    private static string decodeHand(byte handEventByte)
    {
        //we better start using enums for that soon!
        switch (handEventByte)
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
        Position = reader.ReadVector3();
        Subject = reader.ReadNetworkId();

    }

    public override void Serialize(NetworkWriter writer)
    {
        base.Serialize(writer);
        writer.Write(Hand);
        writer.Write(Position);
        writer.Write(Subject);
    }
}

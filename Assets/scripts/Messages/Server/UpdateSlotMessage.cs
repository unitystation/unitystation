using System.Collections;
using UI;
using UnityEngine;
using UnityEngine.Networking;

public class UpdateSlotMessage : ServerMessage<UpdateSlotMessage>
{
    public NetworkInstanceId Recipient;
    public string Slot;
    public NetworkInstanceId ObjectForSlot;

    public override IEnumerator Process()
    {
        //To be run on client
        Debug.Log("Processed " + ToString());

        yield return WaitFor(Recipient, ObjectForSlot);

        UIManager.UpdateSlot(new UISlotObject(Slot, NetworkObjects[1]));

    }

    public static UpdateSlotMessage Send(GameObject recipient, string slot, GameObject objectForSlot = null)
    {
        var msg = new UpdateSlotMessage{
            Recipient = recipient.GetComponent<NetworkIdentity>().netId, //?
            Slot = slot,
            ObjectForSlot = (objectForSlot != null) ? 
                objectForSlot.GetComponent<NetworkIdentity>().netId : NetworkInstanceId.Invalid
        };
        msg.SendTo(recipient);
        return msg;
    }

    public override string ToString()
    {
        return string.Format("[RunMethodMessage Recipient={0} Method={2} Parameter={3} Type={1}]", 
                                                        Recipient, MessageType, Slot, ObjectForSlot);
    }
}

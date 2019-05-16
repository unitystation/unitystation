using System.Collections;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class UpdateSlotMessage : ServerMessage
{
	public static short MessageType = (short) MessageTypes.UpdateSlotMessage;
	public bool ForceRefresh;
	public uint ObjectForSlot;
	public uint Recipient;
	public string ToUUID;
	public string FromUUID;

	public override IEnumerator Process()
	{
		//To be run on client
		//        Logger.Log("Processed " + ToString());

		if (ObjectForSlot == uint.Invalid)
		{
			//Clear slot message
			yield return WaitFor(Recipient);
			InventoryManager.UpdateInvSlot(false, ToUUID, null, FromUUID);
		}
		else
		{
			yield return WaitFor(Recipient, ObjectForSlot);
			InventoryManager.UpdateInvSlot(false, ToUUID, NetworkObjects[1], FromUUID);
		}
	}

	public static UpdateSlotMessage Send(GameObject recipient, string toUUID, string fromUUID, GameObject objectForSlot = null, bool forced = true)
	{
		UpdateSlotMessage msg = new UpdateSlotMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId, //?
			ToUUID = toUUID,
			FromUUID = fromUUID,
			ObjectForSlot = objectForSlot != null ? objectForSlot.GetComponent<NetworkIdentity>().netId : uint.Invalid,
			ForceRefresh = forced
		};
		msg.SendTo(recipient);
		return msg;
	}
}
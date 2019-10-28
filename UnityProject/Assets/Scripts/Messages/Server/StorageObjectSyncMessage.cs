using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
///     Tells client to update certain slot (place an object)
/// </summary>
public class StorageObjectSyncMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.StorageObjectSyncMessage;
	public uint Recipient;
	public uint StorageObj;
	public string Data;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient, StorageObj);
		NetworkObjects[1].GetComponent<StorageObject>().UpdateSlots(JsonUtility.FromJson<InventorySlots>(Data).Slots);
	}

	/// <param name="recipient">Client GO</param>
	/// <param name="storageObj">storage object to update</param>
	/// <param name="newSlots">the new slots the client should have for this storage object</param>
	/// <returns></returns>
	public static StorageObjectSyncMessage Send(GameObject recipient, StorageObject storageObj, List<InventorySlot> newSlots)
	{
		StorageObjectSyncMessage msg = new StorageObjectSyncMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId,
				StorageObj = storageObj.GetComponent<NetworkIdentity>().netId,
				Data = JsonUtility.ToJson(new InventorySlots(newSlots))
		};
		msg.SendTo(recipient);
		return msg;
	}
}


/// <summary>
/// Needed only for json serialization to work
/// </summary>
class InventorySlots
{
	public List<InventorySlot> Slots;

	public InventorySlots(List<InventorySlot> slots)
	{
		Slots = slots;
	}
}
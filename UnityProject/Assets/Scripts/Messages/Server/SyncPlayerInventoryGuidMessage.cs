using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///     Syncs the client owned player UI inventory slots with the server slot UUIDs on new player spawn
/// </summary>
public class SyncPlayerInventoryGuidMessage : ServerMessage
{
	public static short MessageType = (short)MessageTypes.SyncPlayerInventoryGuidMessage;

	public NetworkInstanceId Recipient;
	public string Data;

	public override IEnumerator Process()
	{
		yield return WaitFor(Recipient);
		var playerScript = NetworkObjects[0].GetComponent<PlayerScript>();
		var SlotsList = JsonUtility.FromJson<SyncPlayerInventoryList>(Data);

		if(InventoryManager.AllClientInventorySlots == null){
			yield break;
		}
		for (int i = 0; i < SlotsList.slotsToUpdate.Count; i++)
		{
			int index = InventoryManager.AllClientInventorySlots.FindIndex(
				x => x.SlotName == SlotsList.slotsToUpdate[i].SlotName);
			if(index != -1){
				InventoryManager.AllClientInventorySlots[index].UUID = SlotsList.slotsToUpdate[i].UUID;
				InventoryManager.AllClientInventorySlots[index].Owner = playerScript;
			}
		}
	}

	public static SyncPlayerInventoryGuidMessage Send(
		GameObject recipient, List<InventorySlot> slots)
	{
		var slotsCollection = new SyncPlayerInventoryList(slots);
		SyncPlayerInventoryGuidMessage msg = new SyncPlayerInventoryGuidMessage
		{
			Recipient = recipient.GetComponent<NetworkIdentity>().netId, //?
				Data = JsonUtility.ToJson(slotsCollection),
		};
		msg.SendTo(recipient);
		return msg;
	}
}

[Serializable]
public class SyncPlayerInventoryList
{
	public List<InventorySlot> slotsToUpdate = new List<InventorySlot>();

	public SyncPlayerInventoryList(List<InventorySlot> slots)
	{
		slotsToUpdate = slots;
	}
}
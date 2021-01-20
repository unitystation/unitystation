using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;
using Mirror;


public class AdminInventoryTransferMessage : ClientMessage
{
	public uint FromStorage;
	public int FromSlotIndex;
	public NamedSlot FromNamedSlot;
	public uint ToStorage;
	public int ToSlotIndex;
	public NamedSlot ToNamedSlot;

	public override void Process()
	{
		LoadMultipleObjects(new uint[]{FromStorage, ToStorage});
		if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

		var fromSlot = ItemSlot.Get(NetworkObjects[0].GetComponent<ItemStorage>(), FromNamedSlot, FromSlotIndex);
		var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponent<ItemStorage>(), ToNamedSlot, ToSlotIndex);

		var playerScript = SentByPlayer.Script;
		if(PlayerList.Instance.IsAdmin(playerScript.connectedPlayer.UserId))
		{
			Inventory.ServerTransfer(fromSlot, toSlot);
		}
	}

	public static void Send(ItemSlot fromSlot, ItemSlot toSlot)
	{
		AdminInventoryTransferMessage msg = new AdminInventoryTransferMessage
		{
			FromStorage = fromSlot.ItemStorageNetID,
			FromSlotIndex = fromSlot.SlotIdentifier.SlotIndex,
			FromNamedSlot = fromSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
			ToStorage = toSlot.ItemStorageNetID,
			ToSlotIndex = toSlot.SlotIdentifier.SlotIndex,
			ToNamedSlot = toSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
		};
		msg.Send();
	}
}
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using UnityEngine;
using Mirror;


public class AdminInventoryTransferMessage : ClientMessage
{
	public class AdminInventoryTransferMessageNetMessage : NetworkMessage
	{
		public uint FromStorage;
		public int FromSlotIndex;
		public NamedSlot FromNamedSlot;
		public uint ToStorage;
		public int ToSlotIndex;
		public NamedSlot ToNamedSlot;
	}

	public override void Process<T>(T msg)
	{
		var newMsg = msg as AdminInventoryTransferMessageNetMessage;
		if(newMsg == null) return;

		LoadMultipleObjects(new uint[]{newMsg.FromStorage, newMsg.ToStorage});
		if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

		var fromSlot = ItemSlot.Get(NetworkObjects[0].GetComponent<ItemStorage>(), newMsg.FromNamedSlot, newMsg.FromSlotIndex);
		var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponent<ItemStorage>(), newMsg.ToNamedSlot, newMsg.ToSlotIndex);

		var playerScript = SentByPlayer.Script;
		if(PlayerList.Instance.IsAdmin(playerScript.connectedPlayer.UserId))
		{
			Inventory.ServerTransfer(fromSlot, toSlot);
		}
	}

	public static void Send(ItemSlot fromSlot, ItemSlot toSlot)
	{
		AdminInventoryTransferMessageNetMessage msg = new AdminInventoryTransferMessageNetMessage
		{
			FromStorage = fromSlot.ItemStorageNetID,
			FromSlotIndex = fromSlot.SlotIdentifier.SlotIndex,
			FromNamedSlot = fromSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
			ToStorage = toSlot.ItemStorageNetID,
			ToSlotIndex = toSlot.SlotIdentifier.SlotIndex,
			ToNamedSlot = toSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
		};
		new AdminInventoryTransferMessage().Send(msg);
	}
}
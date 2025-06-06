using System;
using Mirror;

namespace Messages.Client
{
	public class AdminInventoryTransferMessage : ClientMessage<AdminInventoryTransferMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public int FromStorageObjectIndex;
			public uint FromStorage;
			public int FromSlotIndex;
			public NamedSlot FromNamedSlot;
			public int ToStorageObjectIndex;
			public uint ToStorage;
			public int ToSlotIndex;
			public NamedSlot ToNamedSlot;
		}

		public override void Process(NetMessage msg)
		{
			if (HasPermission(TAG.ADMIN_INVENTORY_TRANSFER) == false) return;

			LoadMultipleObjects(new uint[]{msg.FromStorage, msg.ToStorage});
			if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

			if (msg.FromNamedSlot == NamedSlot.none && msg.FromSlotIndex == -1)
			{
				var ToPickUp = NetworkObjects[0].GetComponent<Pickupable>();
				var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponents<ItemStorage>()[msg.ToStorageObjectIndex], msg.ToNamedSlot, msg.ToSlotIndex);

				Inventory.ServerAdd(ToPickUp, toSlot);
			}
			else
			{
				var fromSlot = ItemSlot.Get(NetworkObjects[0].GetComponents<ItemStorage>()[msg.FromStorageObjectIndex], msg.FromNamedSlot, msg.FromSlotIndex);
				var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponents<ItemStorage>()[msg.ToStorageObjectIndex], msg.ToNamedSlot, msg.ToSlotIndex);

				Inventory.ServerTransfer(fromSlot, toSlot);
			}


		}

		public static void Send(ItemSlot fromSlot, ItemSlot toSlot)
		{
			NetMessage msg = new NetMessage
			{
				FromStorageObjectIndex = fromSlot.ItemStorage.IndexOnObject,
				FromStorage = fromSlot.ItemStorageNetID,
				FromSlotIndex = fromSlot.SlotIdentifier.SlotIndex,
				FromNamedSlot = fromSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
				ToStorageObjectIndex = toSlot.ItemStorage.IndexOnObject,
				ToStorage = toSlot.ItemStorageNetID,
				ToSlotIndex = toSlot.SlotIdentifier.SlotIndex,
				ToNamedSlot = toSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
			};

			Send(msg);
		}

		public static void Send(Pickupable From, ItemSlot toSlot)
		{
			NetMessage msg = new NetMessage
			{
				FromStorageObjectIndex = 0, //There is always 1 on it
				FromStorage = From.netId,
				FromSlotIndex = -1,
				FromNamedSlot = NamedSlot.none,
				ToStorageObjectIndex = toSlot.ItemStorage.IndexOnObject,
				ToStorage = toSlot.ItemStorageNetID,
				ToSlotIndex = toSlot.SlotIdentifier.SlotIndex,
				ToNamedSlot = toSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
			};

			Send(msg);
		}
	}
}
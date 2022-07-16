using Mirror;

namespace Messages.Client
{
	public class AdminInventoryTransferMessage : ClientMessage<AdminInventoryTransferMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint FromStorage;
			public int FromSlotIndex;
			public NamedSlot FromNamedSlot;
			public uint ToStorage;
			public int ToSlotIndex;
			public NamedSlot ToNamedSlot;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[]{msg.FromStorage, msg.ToStorage});
			if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

			var fromSlot = ItemSlot.Get(NetworkObjects[0].GetComponent<ItemStorage>(), msg.FromNamedSlot, msg.FromSlotIndex);
			var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponent<ItemStorage>(), msg.ToNamedSlot, msg.ToSlotIndex);

			if (SentByPlayer.IsAdmin)
			{
				Inventory.ServerTransfer(fromSlot, toSlot);
			}
		}

		public static void Send(ItemSlot fromSlot, ItemSlot toSlot)
		{
			NetMessage msg = new NetMessage
			{
				FromStorage = fromSlot.ItemStorageNetID,
				FromSlotIndex = fromSlot.SlotIdentifier.SlotIndex,
				FromNamedSlot = fromSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back),
				ToStorage = toSlot.ItemStorageNetID,
				ToSlotIndex = toSlot.SlotIdentifier.SlotIndex,
				ToNamedSlot = toSlot.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.back)
			};

			Send(msg);
		}
	}
}
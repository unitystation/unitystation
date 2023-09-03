using System.Linq;
using Logs;
using Messages.Server;
using Mirror;

namespace Messages.Client
{
	/// <summary>
	/// Client tells server to transfer items between 2 item slots.
	/// The item slots must be either in the player's slot tree (i.e. currently owned by them
	/// even if nested within an item storage) or in an InteractableStorage that this player
	/// is an observer of.
	/// </summary>
	public class RequestInventoryTransferMessage : ClientMessage<RequestInventoryTransferMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint FromStorage;
			public int FromSlotIndex;
			public NamedSlot FromNamedSlot;
			public int StorageIndexOnGameObjectFrom;
			public uint ToStorage;
			public int ToSlotIndex;
			public NamedSlot ToNamedSlot;
			public int StorageIndexOnGameObjectTo;
		}

		public override void Process(NetMessage msg)
		{
			LoadMultipleObjects(new uint[]{msg.FromStorage, msg.ToStorage});
			if (NetworkObjects[0] == null || NetworkObjects[1] == null) return;

			var fromSlot = ItemSlot.Get(NetworkObjects[0].GetComponents<ItemStorage>()[msg.StorageIndexOnGameObjectFrom], msg.FromNamedSlot, msg.FromSlotIndex);
			var toSlot = ItemSlot.Get(NetworkObjects[1].GetComponents<ItemStorage>()[msg.StorageIndexOnGameObjectTo], msg.ToNamedSlot, msg.ToSlotIndex);

			if (!Validations.CanPutItemToSlot(SentByPlayer.Script, toSlot, fromSlot.Item, NetworkSide.Server, examineRecipient: SentByPlayer.GameObject))
			{
				HandleFail(fromSlot, toSlot);
				return;
			}
			//the slots must both be either in this player's inv or in an observed InteractableStorage
			if (!ValidSlot(toSlot) || !ValidSlot(fromSlot))
			{
				HandleFail(fromSlot, toSlot);
				return;
			}

			Inventory.ServerTransfer(fromSlot, toSlot);
		}

		private bool ValidSlot(ItemSlot toCheck)
		{
			var holder = toCheck.GetRootStorageOrPlayer().gameObject;
			//its in their inventory, this is valid
			if (holder == SentByPlayer.GameObject) return true;

			//it's not in their inventory but they may be observing this in an interactable storage
			var interactableStorage = toCheck.ItemStorage != null ? toCheck.ItemStorage.GetComponents<ItemStorage>() : null;
			if (interactableStorage != null)
			{
				return toCheck.ServerIsObservedBy(SentByPlayer.GameObject);
			}

			return false;
		}

		private void HandleFail(ItemSlot fromSlot, ItemSlot toSlot)
		{
			Loggy.LogWarningFormat(
				"Possible hacking attempt (or bad clientside logic), {0} tried to transfer from slot {1} to {2} when they" +
				" are not allowed.", Category.Exploits, SentByPlayer.GameObject.name, fromSlot, toSlot);

			//roll back the client prediction
			UpdateItemSlotMessage.Send(SentByPlayer.GameObject, fromSlot);
			UpdateItemSlotMessage.Send(SentByPlayer.GameObject, toSlot);
		}

		/// <summary>
		/// For internal inventory system use only. Use Inventory.ClientRequestTransfer to properly request
		/// a transfer.
		///
		/// Client tells server to transfer items between 2 item slots.
		/// One of the item slots must be either in this player's slot tree (i.e. currently owned by them
		/// even if nested within an item storage).
		/// </summary>
		/// <param name="fromSlot">
		/// o</param>
		/// <param name="toSlot"></param>
		/// <returns></returns>
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

			var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;

			//If there's multiple ItemStorage On one game object it can find the correct one by index
			msg.StorageIndexOnGameObjectFrom = 0;
			foreach (var itemStorage in spawned[fromSlot.ItemStorageNetID].GetComponents<ItemStorage>())
			{
				if (itemStorage == fromSlot.ItemStorage)
				{
					break;
				}

				msg.StorageIndexOnGameObjectFrom++;
			}

			msg.StorageIndexOnGameObjectTo = 0;
			foreach (var itemStorage in spawned[toSlot.ItemStorageNetID].GetComponents<ItemStorage>())
			{
				if (itemStorage == toSlot.ItemStorage)
				{
					break;
				}

				msg.StorageIndexOnGameObjectTo++;
			}

			Send(msg);
		}
	}
}
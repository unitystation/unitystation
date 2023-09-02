using Logs;
using Mirror;
using UnityEngine;

namespace Messages.Server
{
	/// <summary>
	/// Message informing a client they are now observing / not observing
	/// a particular InteractableStorage (and/or any children of it) and can show/hide the popup in the UI.
	/// </summary>
	public class ObserveInteractableStorageMessage : ServerMessage<ObserveInteractableStorageMessage.NetMessage>
	{
		public struct NetMessage : NetworkMessage
		{
			public uint Storage;
			public bool Observed;
		}

		public override void Process(NetMessage msg)
		{
			LoadNetworkObject(msg.Storage);

			var storageObject = NetworkObject;
			if (storageObject == null)
			{
				Loggy.LogWarningFormat("Client could not find observed storage with id {0}", Category.Inventory, msg.Storage);
				return;
			}

			var itemStorage = storageObject.GetComponent<ItemStorage>();
			if (msg.Observed)
			{
				UIManager.StorageHandler.OpenStorageUI(itemStorage);
			}
			else
			{
				if (UIManager.StorageHandler.CurrentOpenStorage == itemStorage)
				{
					UIManager.StorageHandler.CloseStorageUI();
				}
				//hide any children they might be viewing as well
				foreach (var slot in itemStorage.GetItemSlotTree())
				{
					if (slot.ItemObject && slot.ItemObject == UIManager.StorageHandler.CurrentOpenStorage?.gameObject)
					{
						UIManager.StorageHandler.CloseStorageUI();
						break;
					}
				}
			}

		}

		/// <summary>
		/// Informs the recipient that they can now show/hide the UI popup for observing a particular
		/// storage or any children.
		/// </summary>
		/// <param name="recipient"></param>
		/// <param name="storage"></param>
		/// <param name="observed">true indicates they should show the popup, false indicates it should be hidden</param>
		public static void Send(GameObject recipient, InteractableStorage storage, bool observed)
		{
			var msg = new NetMessage()
			{
				Storage = storage.gameObject.NetId(),
				Observed = observed
			};

			SendTo(recipient, msg);
		}
	}
}

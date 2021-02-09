
using System.Collections;
using UnityEngine;

/// <summary>
/// Message informing a client they are now observing / not observing
/// a particular InteractableStorage (and/or any children of it) and can show/hide the popup in the UI.
/// </summary>
public class ObserveInteractableStorageMessage : ServerMessage
{
	public uint Storage;
	public bool Observed;

	public override void Process()
	{
		LoadNetworkObject(Storage);

		var storageObject = NetworkObject;
		if (storageObject == null)
		{
			Logger.LogWarningFormat("Client could not find observed storage with id {0}", Category.Inventory, Storage);
			return;
		}

		var itemStorage = storageObject.GetComponent<ItemStorage>();
		if (Observed)
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
				if (slot.ItemObject == UIManager.StorageHandler.CurrentOpenStorage?.gameObject)
				{
					UIManager.StorageHandler.CloseStorageUI();
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
		var msg = new ObserveInteractableStorageMessage()
		{
			Storage = storage.gameObject.NetId(),
			Observed = observed
		};

		msg.SendTo(recipient);
	}
}

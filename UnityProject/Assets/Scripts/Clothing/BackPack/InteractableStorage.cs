using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows a storage object to be interacted with in inventory, to open/close it and drag things.
/// Currently only supports indexed slots.
/// </summary>
[RequireComponent(typeof(ItemStorage))]
[RequireComponent(typeof(Pickupable))]
public class InteractableStorage : MonoBehaviour, IClientInteractable<HandActivate>, IClientInteractable<InventoryApply>,
	ICheckedInteractable<InventoryApply>, IServerInventoryMove
{
	private ItemStorage itemStorage;
	private ObjectBehaviour objectBehaviour;

	void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
		objectBehaviour = GetComponent<ObjectBehaviour>();
	}


	public void OnInventoryMoveServer(InventoryMove info)
	{
		var fromRootStorage = info.FromSlot?.GetRootStorage();
		var toRootStorage = info.FromSlot?.GetRootStorage();
		if (fromRootStorage == toRootStorage)
		{
			//nothing to do, the owner wouldn't have changed
			return;
		}
		GameObject fromPlayer = null;
		GameObject toPlayer = null;
		if (fromRootStorage != null && fromRootStorage.GetComponent<RegisterPlayer>() != null)
		{
			fromPlayer = fromRootStorage.gameObject;
		}
		if (toRootStorage != null && toRootStorage.GetComponent<RegisterPlayer>() != null)
		{
			toPlayer = toRootStorage.gameObject;
		}

		//when this storage changes ownership to another player, the new owner becomes an observer of each slot in the slot
		//tree.
		//When it leaves ownership of another player, the previous owner no longer observes each slot in the slot tree.
		foreach (var slot in itemStorage.GetItemSlotTree())
		{
			if (fromPlayer != null)
			{
				slot.ServerRemoveObserverPlayer(fromPlayer);
			}

			if (toPlayer != null)
			{
				slot.ServerAddObserverPlayer(toPlayer);
			}
		}
	}

	public bool Interact(InventoryApply interaction)
	{
		//client-side inventory apply interaction is just for opening / closing the backpack
		if (interaction.TargetObject != gameObject)
		{
			//backpack can't be "applied" to something else in inventory
			return false;
		}
		if (interaction.HandObject == null)
		{
			//nothing in hand, just open / close the backpack
			return Interact(HandActivate.ByLocalPlayer());
		}
		return false;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		//check if we can store our hand item in this storage.
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//have to have something in hand to try to store it
		if (interaction.HandObject == null) return false;
		//item must be able to fit
		//note: since this is in local player's inventory, we are safe to check this stuff on client side
		var freeSlot = itemStorage.GetNextFreeIndexedSlot();
		if (freeSlot == null) return false;
		if (!freeSlot.CanFit(interaction.HandObject)) return false;

		return true;
	}


	public void ServerPerformInteraction(InventoryApply interaction)
	{
		Inventory.ServerTransfer(interaction.HandSlot,
			itemStorage.GetNextFreeIndexedSlot());
	}

	public bool Interact(HandActivate interaction)
	{
		//open / close the backpack on activate
		if (UIManager.StorageHandler.currentOpenStorage != itemStorage)
		{
			UIManager.StorageHandler.OpenStorageUI(itemStorage);
		}
		else
		{
			UIManager.StorageHandler.CloseStorageUI();
		}

		return true;
	}
}
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
	ICheckedInteractable<InventoryApply>, ICheckedInteractable<MouseDrop>
{
	/// <summary>
	/// Item storage that is being interacted with.
	/// </summary>
	public ItemStorage ItemStorage => itemStorage;

	private ItemStorage itemStorage;

	void Awake()
	{
		itemStorage = GetComponent<ItemStorage>();
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
		if (UIManager.StorageHandler.CurrentOpenStorage != itemStorage)
		{
			UIManager.StorageHandler.OpenStorageUI(itemStorage);
		}
		else
		{
			UIManager.StorageHandler.CloseStorageUI();
		}

		return true;
	}

	public bool WillInteract(MouseDrop interaction, NetworkSide side)
	{
		//can only drag and drop on ourselves
		if (interaction.Performer != interaction.TargetObject) return false;
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		return true;
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{
		//player can observe this storage
		itemStorage.ServerAddObserverPlayer(interaction.Performer);
		ObserveInteractableStorageMessage.Send(interaction.Performer, this, true);
		SpatialRelationship.Activate(RangeRelationship.Between(interaction.Performer, interaction.TargetObject,
			PlayerScript.interactionDistance, ServerOnRelationshipCancelled));
	}

	private void ServerOnRelationshipCancelled(RangeRelationship cancelled)
	{
		//they can't observe anymore
		itemStorage.ServerRemoveObserverPlayer(cancelled.obj1.gameObject);
		ObserveInteractableStorageMessage.Send(cancelled.obj1.gameObject, this, false);
	}
}
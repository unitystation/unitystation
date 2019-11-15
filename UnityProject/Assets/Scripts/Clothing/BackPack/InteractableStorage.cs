using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Allows a storage object to be interacted with in inventory, to open/close it and drag things.
/// Currently only supports indexed slots.
/// </summary>
[RequireComponent(typeof(ItemStorage))]
[RequireComponent(typeof(Pickupable))]
[RequireComponent(typeof(MouseDraggable))]
public class InteractableStorage : MonoBehaviour, IClientInteractable<HandActivate>, IClientInteractable<InventoryApply>,
	ICheckedInteractable<InventoryApply>, ICheckedInteractable<MouseDrop>, IServerInventoryMove
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
		//can only be opened if it's in the player's top level inventory
		if (interaction.TargetSlot.ItemStorage.gameObject != PlayerManager.LocalPlayer) return false;

		if (interaction.UsedObject == null)
		{
			//nothing in hand, just open / close the backpack
			return Interact(HandActivate.ByLocalPlayer());
		}
		return false;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		//we need to be the target - something is put inside us
		if (interaction.TargetObject != gameObject) return false;
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//item must be able to fit
		//note: since this is in local player's inventory, we are safe to check this stuff on client side
		if (!Validations.CanPutItemToStorage(interaction.Performer.GetComponent<PlayerScript>(),
			itemStorage, interaction.UsedObject, side, examineRecipient: interaction.Performer)) return false;

		return true;
	}


	public void ServerPerformInteraction(InventoryApply interaction)
	{
		Inventory.ServerTransfer(interaction.FromSlot,
			itemStorage.GetBestSlotFor(((Interaction) interaction).UsedObject));
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
		if (!DefaultWillInteract.Default(interaction, side)) return false;
		//can't drag / view ourselves
		if (interaction.Performer == interaction.DroppedObject) return false;
		//can only drag and drop the object to ourselves,
		//or from our inventory to this object
		if (interaction.IsFromInventory && interaction.TargetObject == gameObject)
		{
			//trying to add an item from inventory slot to this storage sitting in the world
			return Validations.CanPutItemToStorage(interaction.Performer.GetComponent<PlayerScript>(),
				itemStorage, interaction.DroppedObject.GetComponent<Pickupable>(), side, examineRecipient: interaction.Performer);
		}
		else
		{
			//trying to view this storage, can only drop on ourselves to view it
			if (interaction.Performer != interaction.TargetObject) return false;
			//if we're dragging another player to us, it's only allowed if the other player is downed
			if (Validations.HasComponent<PlayerScript>(interaction.DroppedObject))
			{
				//dragging a player, can only do this if they are down / dead
				return Validations.IsDeadOrCrit(interaction.DroppedObject, side);
			}

			return true;
		}
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{

		if (interaction.IsFromInventory && interaction.TargetObject == gameObject)
		{
			//try to add item to this storage
			Inventory.ServerTransfer(interaction.FromSlot,
				itemStorage.GetBestSlotFor(interaction.UsedObject));
		}
		else
		{
			//player can observe this storage
			itemStorage.ServerAddObserverPlayer(interaction.Performer);
			ObserveInteractableStorageMessage.Send(interaction.Performer, this, true);
			if (!interaction.IsFromInventory)
			{
				SpatialRelationship.ServerActivate(RangeRelationship.Between(interaction.Performer, interaction.UsedObject,
					PlayerScript.interactionDistance, ServerOnRelationshipEnded));
			}
		}

	}

	private void ServerOnRelationshipEnded(RangeRelationship cancelled)
	{
		//they can't observe anymore
		itemStorage.ServerRemoveObserverPlayer(cancelled.obj1.gameObject);
		ObserveInteractableStorageMessage.Send(cancelled.obj1.gameObject, this, false);
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//stop player observing if it's dropped from the player's storage
		var fromRootPlayer = info.FromRootPlayer;
		var toRootPlayer = info.ToRootPlayer;
		//no need to do anything, hasn't moved into player inventory
		if (fromRootPlayer == toRootPlayer) return;

		//make sure it's closed and any children as well
		if (fromRootPlayer != null)
		{
			ObserveInteractableStorageMessage.Send(fromRootPlayer.gameObject, this, false);
		}
	}
}
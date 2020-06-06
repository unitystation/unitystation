using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
/// <summary>
/// Allows a storage object to be interacted with, to open/close it and drag things. Works for
/// player inventories and normal indexed storages like backpacks
/// </summary>
[RequireComponent(typeof(ItemStorage))]
[RequireComponent(typeof(MouseDraggable))]
//[RequireComponent(typeof(ActionControlInventory))] removed because the PDA wont need it
public class InteractableStorage : MonoBehaviour, IClientInteractable<HandActivate>, IClientInteractable<InventoryApply>,
	ICheckedInteractable<InventoryApply>, ICheckedInteractable<PositionalHandApply>, ICheckedInteractable<MouseDrop>,
	IServerInventoryMove, IClientInventoryMove, IActionGUI
{

	/// <summary>
	/// The click pickup mode.
	/// Single picks up one clicked item.
	/// Same picks up all items of the same type on a tile.
	/// All picks up all items on a tile
	/// </summary>
	enum PickupMode
	{
		Single,
		Same,
		All,
	}

	[Tooltip(
		"Add storage items that should prohibited from being added to storage of this storage item (like tool boxes" +
		"being placed inside other tool boxes")]
	public StorageItemName denyStorageOfStorageItems;

	/// <summary>
	/// Item storage that is being interacted with.
	/// </summary>
	public ItemStorage ItemStorage => itemStorage;

	private ItemStorage itemStorage;

	/// <summary>
	/// Flag to determine if this can store items by clicking on them
	/// </summary>
	[SerializeField]
	[Tooltip("Can you store items by clicking on them with this in hand?")]
	private bool canClickPickup = false;

	/// <summary>
	/// Flag to determine if this can empty out all items by activating it
	/// </summary>
	[SerializeField]
	[Tooltip("Can you empty out all items by activating this item?")]
	private bool canQuickEmpty = false;

	/// <summary>
	/// The current pickup mode used when clicking
	/// </summary>
	private PickupMode pickupMode = PickupMode.All;

	private bool allowedToInteract = false;


	[SerializeField]
	private ActionData actionData = null;
	public ActionData ActionData => actionData;

	/// <summary>
	/// Used on the server to switch the pickup mode of this InteractableStorage
	/// </summary>
	public void ServerSwitchPickupMode(GameObject player)
	{
		pickupMode = pickupMode.Next();

		string msg = "Nothing happens.";
		switch (pickupMode)
		{
			case PickupMode.Single:
				msg = $"The {gameObject.ExpensiveName()} now picks up one item at a time.";
				break;
			case PickupMode.Same:
				msg = $"The {gameObject.ExpensiveName()} now picks up all items of a single type at once.";
				break;
			case PickupMode.All:
				msg = $"The {gameObject.ExpensiveName()} now picks up all items in a tile at once.";
				break;
			default:
				Logger.LogError($"Unknown pickup mode set! Found: {pickupMode}", Category.Inventory);
				break;
		}
		Chat.AddExamineMsgFromServer(player, msg);
	}

	private void OnEnable()
	{
		allowedToInteract = false;
		itemStorage = GetComponent<ItemStorage>();
		StartCoroutine(SpawnCoolDown());
	}

	//Trying to prevent auto click spam exploit when storage is being populated
	IEnumerator SpawnCoolDown()
	{
		yield return WaitFor.Seconds(0.2f);
		allowedToInteract = true;
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
		if (!allowedToInteract) return false;
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
		if (!allowedToInteract) return;
		Inventory.ServerTransfer(interaction.FromSlot,
			itemStorage.GetBestSlotFor(((Interaction)interaction).UsedObject));
	}

	/// <summary>
	/// Client:
	/// Allow items to be stored by clicking on bags with item in hand
	/// and clicking items with bag in hand if CanClickPickup is enabled
	/// </summary>
	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (!allowedToInteract) return false;
		// Use default interaction checks
		if (!DefaultWillInteract.Default(interaction, side)) return false;

		// See which item needs to be stored
		if (Validations.IsTarget(gameObject, interaction))
		{
			// We're the target
			// If player's hands are empty let Pickupable handle the interaction
			if (interaction.HandObject == null) return false;

			// There's something in the player's hands
			// Check if item from the hand slot fits in this storage sitting in the world
			if (!Validations.CanPutItemToStorage(interaction.PerformerPlayerScript,
			itemStorage, interaction.HandObject, side, examineRecipient: interaction.Performer))
			{
				Chat.AddExamineMsgToClient($"The {interaction.HandObject.ExpensiveName()} doesn't fit!");
				return false;
			}
			return true;
		}
		else if (canClickPickup)
		{
			// If we can click pickup then try to store the target object
			switch (pickupMode)
			{
				case PickupMode.Single:
					// See if there's an item to pickup
					if (interaction.TargetObject == null ||
						interaction.TargetObject.Item() == null)
					{
						Chat.AddExamineMsgToClient("There's nothing to pickup!");
						return false;
					}
					if (!Validations.CanPutItemToStorage(interaction.PerformerPlayerScript,
							itemStorage, interaction.TargetObject, side, examineRecipient: interaction.Performer))
					{
						// In Single pickup mode if the target item doesn't
						// fit then don't interact
						Chat.AddExamineMsgToClient($"The {interaction.TargetObject.ExpensiveName()} doesn't fit!");
						return false;
					}
					break;
				case PickupMode.Same:
					if (interaction.TargetObject == null)
					{
						// If there's nothing to compare then don't interact
						Chat.AddExamineMsgToClient("There's nothing to pickup!");
						return false;
					}
					break;
			}
			// In Same and All pickup modes other items on the
			// tile could still be picked up, so we interact
			return true;
		}
		else
		{
			// We're not the target and we can't click pickup so don't do anything
			return false;
		}
	}

	/// <summary>
	/// Server:
	/// Allow items to be stored by clicking on bags with item in hand
	/// and clicking items with bag in hand if CanClickPickup is enabled
	///
	/// </summary>
	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (!allowedToInteract) return;
		// See which item needs to be stored
		if (Validations.IsTarget(gameObject, interaction))
		{
			// Add hand item to this storage
			Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetBestSlotFor(interaction.HandObject));
		}
		// See if this item can click pickup
		else if (canClickPickup)
		{
			bool pickedUpSomething = false;
			Pickupable pickup;
			switch (pickupMode)
			{
				case PickupMode.Single:

					// Don't pick up items which aren't set as CanPickup
					pickup = interaction.TargetObject.GetComponent<Pickupable>();
					if (pickup == null || pickup.CanPickup == false)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "There's nothing to pickup!");
						return;
					}
					// Store the clicked item
					var slot = itemStorage.GetBestSlotFor(interaction.TargetObject);
					if (slot == null)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer,
							$"The {interaction.TargetObject.ExpensiveName()} doesn't fit!");
						return;
					}
					Inventory.ServerAdd(interaction.TargetObject, slot);
					break;

				case PickupMode.Same:
					if (interaction.TargetObject == null ||
						interaction.TargetObject.Item() == null)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "There's nothing to pickup!");
						return;
					}

					// Get all items of the same type on the tile and try to store them
					var itemsOnTileSame = MatrixManager.GetAt<ItemAttributesV2>(interaction.WorldPositionTarget.To2Int().To3Int(), true);

					if (itemsOnTileSame.Count == 0)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "There's nothing to pickup!");
						return;
					}

					foreach (var item in itemsOnTileSame)
					{

						// Don't pick up items which aren't set as CanPickup
						pickup = item.gameObject.GetComponent<Pickupable>();
						if (pickup == null || pickup.CanPickup == false)
						{
							continue;
						}

						// Only try to add it if it matches the target object's traits
						if (item.HasAllTraits(interaction.TargetObject.Item().GetTraits()))
						{
							// Try to add each item to the storage
							// Can't break this loop when it fails because some items might not fit and
							// there might be stacks with space still
							if (Inventory.ServerAdd(item.gameObject, itemStorage.GetBestSlotFor(item.gameObject)))
							{
								pickedUpSomething = true;
							}
						}
					}
					Chat.AddExamineMsgFromServer(interaction.Performer, $"You put everything you could in the {gameObject.ExpensiveName()}.");
					break;

				case PickupMode.All:
					// Get all items on the tile and try to store them
					var itemsOnTileAll = MatrixManager.GetAt<ItemAttributesV2>(interaction.WorldPositionTarget.To2Int().To3Int(), true);

					if (itemsOnTileAll.Count == 0)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "There's nothing to pickup!");
						return;
					}

					foreach (var item in itemsOnTileAll)
					{
						// Don't pick up items which aren't set as CanPickup
						pickup = item.gameObject.GetComponent<Pickupable>();
						if (pickup == null || pickup.CanPickup == false)
						{
							continue;
						}
						// Try to add each item to the storage
						// Can't break this loop when it fails because some items might not fit and
						// there might be stacks with space still
						if (Inventory.ServerAdd(item.gameObject, itemStorage.GetBestSlotFor(item.gameObject)))
						{
							pickedUpSomething = true;

						}
					}
					if (pickedUpSomething)
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, $"You put everything you could in the {gameObject.ExpensiveName()}.");
					}
					else
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "There's nothing to pickup!");
					}
					break;
			}
		}
	}

	//This is all client only interaction:
	public bool Interact(HandActivate interaction)
	{
		if (canQuickEmpty)
		{
			// Drop all items that are inside this storage
			var slots = itemStorage.GetItemSlots();

			if (slots == null)
			{
				if (!CustomNetworkManager.Instance._isServer)
				{
					Chat.AddExamineMsgToClient("It's already empty!");
				}

				return false;
			}

			if (PlayerManager.PlayerScript == null) return false;

			PlayerManager.PlayerScript.playerNetworkActions.CmdDropAllItems(itemStorage.GetIndexedItemSlot(0).ItemStorageNetID);

			if (!CustomNetworkManager.Instance._isServer)
			{
				Chat.AddExamineMsgToClient($"You start dumping out the {gameObject.ExpensiveName()}.");
			}

			return true;
		}

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
		if (!allowedToInteract) return false;
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
				return Validations.IsStrippable(interaction.DroppedObject, side);
			}

			return true;
		}
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{
		if (!allowedToInteract) return;
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

			//if we are observing a storage not in our inventory (such as another player's top
			//level inventory or a storage within their inventory, or a box/backpack sitting on the ground), we must stop observing when it
			//becomes unobservable for whatever reason (such as the owner becoming unobservable)
			var rootStorage = itemStorage.GetRootStorage();
			if (interaction.Performer != rootStorage.gameObject)
			{
				//stop observing when it becomes unobservable for whatever reason
				var relationship = ObserveStorageRelationship.Observe(this, interaction.Performer.GetComponent<RegisterPlayer>(),
					PlayerScript.interactionDistance, ServerOnObservationEnded);
				SpatialRelationship.ServerActivate(relationship);
			}
		}

	}

	private void ServerOnObservationEnded(ObserveStorageRelationship cancelled)
	{
		//they can't observe anymore
		itemStorage.ServerRemoveObserverPlayer(cancelled.ObserverPlayer.gameObject);
		ObserveInteractableStorageMessage.Send(cancelled.ObserverPlayer.gameObject, this, false);
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//stop any observers (except for owner) from observing it if it's moved
		var fromRootPlayer = info.FromRootPlayer;
		if (fromRootPlayer != null)
		{
			itemStorage.ServerRemoveAllObserversExceptOwner();
		}

		//stop owner observing if it's dropped from the owner's storage
		var toRootPlayer = info.ToRootPlayer;
		//no need to do anything, hasn't moved into player inventory
		if (fromRootPlayer == toRootPlayer) return;

		//make sure it's closed and any children as well
		if (fromRootPlayer != null)
		{
			ObserveInteractableStorageMessage.Send(fromRootPlayer.gameObject, this, false);
		}
	}

	// TODO: this should be merged into a new AlertUI action system once it's implemented
	// Client only method
	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		if (CustomNetworkManager.Instance._isServer && GameData.IsHeadlessServer)
			return;

		if (canClickPickup)
		{
			// Show the 'switch pickup mode' action button if this is in either of the players hands
			var pna = PlayerManager.LocalPlayerScript.playerNetworkActions;
			var showAlert = pna.GetActiveHandItem() == gameObject ||
							pna.GetOffHandItem() == gameObject;

			UIActionManager.ToggleLocal(this, showAlert);
		}
	}

	public void CallActionClient()
	{
		PlayerManager.PlayerScript.playerNetworkActions.CmdSwitchPickupMode();

	}
}
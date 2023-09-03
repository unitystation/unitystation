using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Items;
using Logs;
using Messages.Server;
using Objects.Disposals;
using UI.Core.Action;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

/// <summary>
/// Allows a storage object to be interacted with, to open/close it and drag things. Works for
/// player inventories and normal indexed storages like backpacks
/// </summary>
[RequireComponent(typeof(ItemStorage))]
[RequireComponent(typeof(MouseDraggable))]
//[RequireComponent(typeof(ActionControlInventory))] removed because the PDA wont need it
public class InteractableStorage : MonoBehaviour, IClientInteractable<HandActivate>,
	IClientInteractable<InventoryApply>,
	ICheckedInteractable<InventoryApply>, ICheckedInteractable<PositionalHandApply>,
	ICheckedInteractable<HandApply>, ICheckedInteractable<MouseDrop>, IActionGUI, IItemInOutMovedPlayer, IHoverTooltip
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
		DropClick
	}

	[Tooltip(
		"Add storage items that should prohibited from being added to storage of this storage item (like tool boxes" +
		"being placed inside other tool boxes")]
	public StorageItemName denyStorageOfStorageItems;

	/// <summary>
	/// Item storage that is being interacted with.
	/// </summary>
	public ItemStorage ItemStorage => itemStorage;

	[SerializeField]
	private ItemStorage itemStorage;

	/// <summary>
	/// Flag to determine if this can store items by clicking on them
	/// </summary>
	[SerializeField] [Tooltip("Can you store items by clicking on them with this in hand?")]
	private bool canClickPickup = false;

	/// <summary>
	/// Flag to determine if this can empty out all items by activating it
	/// </summary>
	[SerializeField] [Tooltip("Can you empty out all items by activating this item?")]
	private bool canQuickEmpty = false;

	[SerializeField] [Tooltip("Does it require alt click When in top-level inventory")]
	private bool TopLevelAlt = false;

	/// <summary>
	/// The current pickup mode used when clicking
	/// </summary>
	private PickupMode pickupMode = PickupMode.All;

	private bool allowedToInteract = false;

	private CooldownInstance cooldown = new CooldownInstance(0.5f);

	[SerializeField] private ActionData actionData = null;
	public ActionData ActionData => actionData;

	private bool preventUIShowingAfterTrapTrigger = false;

	public bool PreventUIShowingAfterTrapTrigger
	{
		get => preventUIShowingAfterTrapTrigger;
		set => preventUIShowingAfterTrapTrigger = value;
	}


	public bool DoNotShowInventoryOnUI = false;

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
			case PickupMode.DropClick:
				msg = $"The {gameObject.ExpensiveName()} now drops all items on the tile at once";
				break;
			default:
				Loggy.LogError($"Unknown pickup mode set! Found: {pickupMode}", Category.Inventory);
				break;
		}

		Chat.AddExamineMsgFromServer(player, msg);
	}

	private void OnEnable()
	{
		allowedToInteract = false;
		if (itemStorage == null)
		{
			itemStorage = GetComponent<ItemStorage>();
		}

		StartCoroutine(SpawnCoolDown());
	}

	// Trying to prevent auto click spam exploit when storage is being populated
	private IEnumerator SpawnCoolDown()
	{
		yield return WaitFor.Seconds(0.2f);
		allowedToInteract = true;
	}

	private bool IsFull(GameObject usedObject, GameObject player, bool noMessage = false)
	{
		//NOTE: this wont fail on client if the storage they are checking is not being observered by them
		if (itemStorage.GetBestSlotFor(usedObject) == null && usedObject != null)
		{
			if (noMessage == false)
			{
				Chat.AddExamineMsg(player,
					$"<color=red>The {usedObject.ExpensiveName()} won't fit in the {itemStorage.gameObject.ExpensiveName()}. Make some space!</color>");
			}

			return true;
		}

		return false;
	}

	public bool Interact(InventoryApply interaction)
	{
		// client-side inventory apply interaction is just for opening / closing the backpack
		if (interaction.TargetObject != gameObject)
		{
			//backpack can't be "applied" to something else in inventory
			return false;
		}

		// can only be opened if it's in the player's top level inventory or player is alt-clicking
		if ((PlayerManager.LocalPlayerScript.DynamicItemStorage.ClientTotal.Contains(interaction.TargetSlot) &&
		     TopLevelAlt == false) || interaction.IsAltClick && DoNotShowInventoryOnUI == false)
		{
			if (interaction.UsedObject == null)
			{
				// nothing in hand, just open / close the backpack
				return Interact(HandActivate.ByLocalPlayer());
			}
		}

		return false;
	}

	public bool WillInteract(InventoryApply interaction, NetworkSide side)
	{
		if (allowedToInteract == false) return false;
		// we need to be the target - something is put inside us
		if (interaction.TargetObject != gameObject) return false;
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (Cooldowns.IsOn(interaction, cooldown, side)) return false;

		if (IsFull(interaction.UsedObject, interaction.Performer))
		{
			if (Cooldowns.TryStart(interaction, cooldown, side) == false) return false;

			return false;
		}

		// item must be able to fit
		// note: since this is in local player's inventory, we are safe to check this stuff on client side
		if (!Validations.CanPutItemToStorage(interaction.Performer.GetComponent<PlayerScript>(),
			    itemStorage, interaction.UsedObject, side, examineRecipient: interaction.Performer)) return false;

		return true;
	}

	public void ServerPerformInteraction(InventoryApply interaction)
	{
		if (allowedToInteract == false) return;
		Inventory.ServerTransfer(interaction.FromSlot,
			itemStorage.GetBestSlotFor((interaction).UsedObject));
		if (interaction.UsedObject.Item().InventoryMoveSound != null)
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(interaction.UsedObject.Item().InventoryMoveSound,
				interaction.Performer.AssumedWorldPosServer());
		}
	}

	/// <summary>
	/// Client:
	/// Allows player to open / close bags in reach using alt-click
	/// </summary>
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (allowedToInteract == false) return false;
		// Use default interaction checks
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (interaction.IsHighlight == false && Cooldowns.IsOn(interaction, cooldown, side)) return false;

		if (IsFull(interaction.UsedObject, interaction.Performer, interaction.IsHighlight))
		{
			if (interaction.IsHighlight == false &&
			    Cooldowns.TryStart(interaction, cooldown, side) == false) return false;

			return false;
		}

		// See which item needs to be stored
		if (Validations.IsTarget(gameObject, interaction))
		{
			// We're the target
			if (interaction.HandObject == null)
			{
				// If player's hands are empty and alt-click let them open the bag
				if (interaction.IsAltClick)
				{
					return true;
				}
			}
			else
			{
				//We have something in our hand, try to put it in;
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Server:
	/// Allows player to open / close bags in reach using alt-click
	/// </summary>
	public void ServerPerformInteraction(HandApply interaction)
	{
		if (interaction.HandObject == null)
		{
			// Reusing mouse drop logic for efficiency
			ServerPerformInteraction(
				MouseDrop.ByClient(interaction.Performer,
					interaction.TargetObject,
					interaction.Performer,
					interaction.Intent,
					interaction.PerformerMind));
			return;
		}

		// Reusing mouse drop logic for efficiency
		ServerPerformInteraction(
			MouseDrop.ByClient(interaction.Performer,
				interaction.UsedObject,
				interaction.TargetObject,
				interaction.Intent,
				interaction.PerformerMind));
	}

	/// <summary>
	/// Client:
	/// Allow items to be stored by clicking on bags with item in hand
	/// and clicking items with bag in hand if CanClickPickup is enabled
	/// </summary>
	public bool WillInteract(PositionalHandApply interaction, NetworkSide side)
	{
		if (allowedToInteract == false) return false;
		// Use default interaction checks
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		if (interaction.TargetObject != null && interaction.TargetObject.HasComponent<DisposalBin>()) return false;

		// See which item needs to be stored
		if (Validations.IsTarget(gameObject, interaction))
		{
			// We're the target
			// If player's hands are empty let Pickupable handle the interaction
			if (interaction.HandObject == null) return false;

			// There's something in the player's hands
			// Check if item from the hand slot fits in this storage sitting in the world
			if (Validations.CanPutItemToStorage(interaction.PerformerPlayerScript,
				    itemStorage, interaction.HandObject, side, examineRecipient: interaction.Performer) == false)
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
	/// </summary>
	public void ServerPerformInteraction(PositionalHandApply interaction)
	{
		if (allowedToInteract == false) return;
		// See which item needs to be stored
		if (Validations.IsTarget(gameObject, interaction))
		{
			// Add hand item to this storage
			Inventory.ServerTransfer(interaction.HandSlot, itemStorage.GetBestSlotFor(interaction.HandObject));
			if (interaction.UsedObject.Item().InventoryMoveSound != null)
			{
				_ = SoundManager.PlayNetworkedAtPosAsync(interaction.UsedObject.Item().InventoryMoveSound,
					interaction.Performer.AssumedWorldPosServer());
			}
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
					var itemsOnTileSame =
						MatrixManager.GetAt<ItemAttributesV2>(interaction.WorldPositionTarget.RoundTo2Int().To3Int(),
							true);

					if (itemsOnTileSame is List<ItemAttributesV2> == false)
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

					Chat.AddExamineMsgFromServer(interaction.Performer,
						$"You put everything you could in the {gameObject.ExpensiveName()}.");
					break;

				case PickupMode.All:
					// Get all items on the tile and try to store them
					var itemsOnTileAll =
						MatrixManager.GetAt<ItemAttributesV2>(interaction.WorldPositionTarget.RoundTo2Int().To3Int(),
							true);

					if (itemsOnTileAll is List<ItemAttributesV2> == false)
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
						Chat.AddExamineMsgFromServer(interaction.Performer,
							$"You put everything you could in the {gameObject.ExpensiveName()}.");
					}
					else
					{
						Chat.AddExamineMsgFromServer(interaction.Performer, "There's nothing to pickup!");
					}

					break;
				case PickupMode.DropClick:
					if (canQuickEmpty)
					{
						// Drop all items that are inside this storage
						var slots = itemStorage.GetItemSlots();
						if (slots == null)
						{
							Chat.AddExamineMsgFromServer(interaction.Performer, "It's already empty!");


							return;
						}

						if (PlayerManager.LocalPlayerScript == null) return;
						if (Validations.IsInReachDistanceByPositions(
							    PlayerManager.LocalPlayerScript.RegisterPlayer.WorldPosition,
							    interaction.WorldPositionTarget) == false) return;
						if (MatrixManager.IsPassableAtAllMatricesOneTile(interaction.WorldPositionTarget.RoundToInt(),
							    CustomNetworkManager.Instance._isServer) == false) return;

						PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdDropAllItems(itemStorage
							.GetIndexedItemSlot(0)
							.ItemStorageNetID, interaction.WorldPositionTarget);


						Chat.AddExamineMsgFromServer(interaction.Performer,
							$"You start dumping out the {gameObject.ExpensiveName()}.");
					}

					break;
			}
		}
	}

	// This is all client only interaction:
	public bool Interact(HandActivate interaction)
	{
		var slots = itemStorage.GetItemSlots();
		if (canQuickEmpty)
		{
			// Drop all items that are inside this storage

			if (slots == null)
			{
				if (!CustomNetworkManager.Instance._isServer)
				{
					Chat.AddExamineMsgToClient("It's already empty!");
				}

				return false;
			}

			if (PlayerManager.LocalPlayerScript == null) return false;

			PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdDropAllItems(itemStorage.GetIndexedItemSlot(0)
				.ItemStorageNetID, TransformState.HiddenPos);

			if (CustomNetworkManager.Instance._isServer == false)
			{
				Chat.AddExamineMsgToClient($"You start dumping out the {gameObject.ExpensiveName()}.");
			}

			return true;
		}

		if (interaction.Intent != Intent.Disarm)
		{
			interaction.PerformerPlayerScript.PlayerNetworkActions.CmdTriggerStorageTrap(gameObject);
			if (PreventUIShowingAfterTrapTrigger)
			{
				preventUIShowingAfterTrapTrigger = false;
				return false;
			}
		}

		// open / close the backpack on activate
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
		if (allowedToInteract == false) return false;
		if (DefaultWillInteract.Default(interaction, side) == false) return false;

		//Can't drag / view ourselves
		if (interaction.Performer == interaction.DroppedObject) return false;

		//Can only drag and drop the object to ourselves, or from our inventory to this object
		if (interaction.IsFromInventory && interaction.TargetObject == gameObject)
		{
			//Trying to add an item from inventory slot to this storage sitting in the world
			return Validations.CanPutItemToStorage(interaction.Performer.GetComponent<PlayerScript>(),
				itemStorage, interaction.DroppedObject.GetComponent<Pickupable>(), side,
				examineRecipient: interaction.Performer);
		}

		//Trying to view this storage, can only drop on ourselves to view it
		if (interaction.Performer != interaction.TargetObject) return false;

		//If we're dragging another player to us, it's only allowed if the other player is downed
		if (Validations.HasComponent<PlayerScript>(interaction.DroppedObject))
		{
			//Dragging a player, can only do this if they are down / dead
			return Validations.IsStrippable(interaction.DroppedObject, side);
		}

		return true;
	}

	public void ServerPerformInteraction(MouseDrop interaction)
	{
		if (allowedToInteract == false) return;
		if (interaction.IsFromInventory && interaction.TargetObject == gameObject)
		{
			// try to add item to this storage
			Inventory.ServerTransfer(interaction.FromSlot,
				itemStorage.GetBestSlotFor(interaction.UsedObject));
		}
		else
		{
			if (DoNotShowInventoryOnUI) return;
			// player can observe this storage
			itemStorage.ServerAddObserverPlayer(interaction.Performer);
			ObserveInteractableStorageMessage.Send(interaction.Performer, this, true);

			// if we are observing a storage not in our inventory (such as another player's top
			// level inventory or a storage within their inventory, or a box/backpack sitting on the ground), we must stop observing when it
			// becomes unobservable for whatever reason (such as the owner becoming unobservable)
			var rootStorage = itemStorage.GetRootStorageOrPlayer();
			if (interaction.Performer != rootStorage.gameObject)
			{
				// stop observing when it becomes unobservable for whatever reason
				var relationship = ObserveStorageRelationship.Observe(this,
					interaction.Performer.GetComponent<RegisterPlayer>(),
					PlayerScript.INTERACTION_DISTANCE, ServerOnObservationEnded);
				SpatialRelationship.ServerActivate(relationship);
			}
		}
	}

	private void ServerOnObservationEnded(ObserveStorageRelationship cancelled)
	{
		// they can't observe anymore
		itemStorage.ServerRemoveObserverPlayer(cancelled.ObserverPlayer.gameObject);
		ObserveInteractableStorageMessage.Send(cancelled.ObserverPlayer.gameObject, this, false);
	}

	public RegisterPlayer CurrentlyOn { get; set; }
	bool IItemInOutMovedPlayer.PreviousSetValid { get; set; }

	public bool IsValidSetup(RegisterPlayer player)
	{
		if (player == null) return false;
		if (canClickPickup)
		{
			foreach (var itemSlot in player.PlayerScript.DynamicItemStorage.GetHandSlots())
			{
				if (itemSlot.ItemObject == gameObject)
				{
					return true;
				}
			}
		}

		return false;
	}

	void IItemInOutMovedPlayer.ChangingPlayer(RegisterPlayer hideForPlayer, RegisterPlayer showForPlayer)
	{
		if (canClickPickup)
		{
			if (hideForPlayer != null)
			{
				UIActionManager.ToggleServer(hideForPlayer.gameObject, this, false);
				itemStorage.ServerRemoveAllObserversExceptOwner();
				ObserveInteractableStorageMessage.Send(hideForPlayer.PlayerScript.gameObject, this, false);
			}

			if (showForPlayer != null)
			{
				itemStorage.ServerAddObserverPlayer(showForPlayer.PlayerScript.gameObject);
				UIActionManager.ToggleServer(showForPlayer.gameObject, this, true);
			}
		}
	}

	public void CallActionClient()
	{
		PlayerManager.LocalPlayerScript.PlayerNetworkActions.CmdSwitchPickupMode();
	}

	public string HoverTip()
	{
		if (itemStorage == null) return null;
		var slots = itemStorage.GetItemSlots().ToList();
		return slots.Any() == false ? null : $"This has {slots.Count()} slots.";
	}

	public string CustomTitle() => null;

	public Sprite CustomIcon() => null;

	public List<Sprite> IconIndicators() => null;

	public List<TextColor> InteractionsStrings()
	{
		var interactions = new List<TextColor>()
		{
			new()
			{
				Text = canQuickEmpty
					? $"Press {KeybindManager.Instance.userKeybinds[KeyAction.HandActivate].PrimaryCombo} or click to quickly empty"
					: "",
				Color = Color.green
			},
			new()
			{
				Text = TopLevelAlt
					? $"Alt+Click with a free hand to access storage."
					: "",
				Color = Color.green
			}
		};

		return interactions;
	}
}
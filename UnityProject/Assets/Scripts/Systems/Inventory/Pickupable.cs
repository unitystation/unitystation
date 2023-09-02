using System;
using Messages.Server;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using Items;
using Logs;
using UI;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// Main API / component for dealing with inventory. Use this if you want to do something to this object that involves
/// inventory. No longer should use InventoryManager.
/// Allows object to be picked up into inventory and moved to different slots, storage objects, and be dropped.
/// </summary>
public class Pickupable : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>,
	IRightClickable, IServerDespawn, IServerInventoryMove
{
	[SerializeField, Tooltip("The speed of the pickup animation.")]
	private float pickupAnimSpeed = 0.2f;

	private UniversalObjectPhysics universalObjectPhysics;
	public UniversalObjectPhysics UniversalObjectPhysics => universalObjectPhysics;

	// controls whether this can currently be picked up.
	[SyncVar]
	private bool canPickup = true;

	/// <summary>
	/// Whether this object can currently be picked up.
	/// </summary>
	public bool CanPickup => canPickup;

	/// <summary>
	/// Which inventory slot this is currently in, null if not in inventory
	/// </summary>
	public ItemSlot ItemSlot => itemSlot;
	private ItemSlot itemSlot;

	[SyncVar] private uint clientSynchronisedStorageIn;

	public GameObject StoredInItemStorageNetworked
	{
		get
		{
			if (isServer)
			{
				return ItemSlot?.ItemStorage.OrNull()?.gameObject;
			}
			var spawned = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
			if (clientSynchronisedStorageIn is NetId.Empty or NetId.Invalid)
			{
				return null;
			}

			if (spawned.ContainsKey(clientSynchronisedStorageIn))
			{
				return spawned[clientSynchronisedStorageIn].gameObject;
			}
			else
			{
				return null;
			}
		}
	}

	/// <summary>
	/// If this item is in a slot linked to a UI slot, returns that UI slot.
	/// </summary>
	public UI_ItemSlot LocalUISlot => itemSlot != null ? ItemSlot.LocalUISlot : null;

	public ItemAttributesV2 ItemAttributesV2;

	/// <summary>
	/// Client Side Events. Expects an interactor.
	/// </summary>
	public UnityEvent<GameObject> OnMoveToPlayerInventory;
	public UnityEvent<GameObject> OnInventoryMoveServerEvent;

	public UnityEvent<GameObject> OnDrop;
	public UnityEvent<GameObject> OnThrow;

	[SerializeField] private LastTouch lastTouch;



	#region Lifecycle

	private void Awake()
	{
		ItemAttributesV2 =  GetComponent<ItemAttributesV2>();
		universalObjectPhysics = GetComponent<UniversalObjectPhysics>();
		if (lastTouch == null) lastTouch = GetComponent<LastTouch>();
	}

	// make sure to call this in subclasses
	public virtual void Start()
	{
		CheckSpriteOrder();
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//remove ourselves from inventory if we aren't already removed
		if (itemSlot != null)
		{
			Inventory.ServerDespawn(itemSlot);
		}
		OnMoveToPlayerInventory?.RemoveAllListeners();
		OnInventoryMoveServerEvent?.RemoveAllListeners();
		OnDrop?.RemoveAllListeners();
		OnThrow?.RemoveAllListeners();
	}

	#endregion

	private ItemSlot RecordedItemSlot;

	public virtual void OnInventoryMoveServer(InventoryMove info)
	{
		/*
		 * TODO: There is a security issue here which existed even prior to inventory refactor.
		 * The issue is that every time a player's top level inventory changes, a message is sent to all other players
		 * telling them what object was added / removed from that player's slot. So with a hacked client,
		 * it would be easy to see every item change that happens on the server in top level inventory, such as being able
		 * to see who is using antag items.
		 *
		 * Bubbling should help prevent this
		 */

		//update appearance depending on the slot that was changed
		if (info.FromPlayer != null &&
		    HasClothingItem(info.FromPlayer,RecordedItemSlot))
		{
			//clear previous slot appearance
			PlayerAppearanceMessage.SendToAll(info.FromPlayer.gameObject,
				(int)RecordedItemSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), null);

			//ask target playerscript to update shown name.
			info.FromPlayer.GetComponent<PlayerScript>().RefreshVisibleName();
		}

		//Handle setting slot
		if (info.MovedObject == this)
		{
			RecordedItemSlot = info.ToSlot;
		}


		if (info.ToPlayer != null &&
			HasClothingItem(info.ToPlayer, RecordedItemSlot))
		{
			//change appearance based on new item
			PlayerAppearanceMessage.SendToAll(info.ToPlayer.gameObject,
				(int)RecordedItemSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), this.gameObject);

			//ask target playerscript to update shown name.
			info.ToPlayer.GetComponent<PlayerScript>().RefreshVisibleName();
		}
		OnInventoryMoveServerEvent?.Invoke(gameObject);

		switch (info.RemoveType)
		{
			case InventoryRemoveType.Drop:
				OnDrop?.Invoke(gameObject);
				break;
			case InventoryRemoveType.Throw:
				OnThrow?.Invoke(gameObject);
				break;
		}
	}

	private bool HasClothingItem(RegisterPlayer onPlayer, ItemSlot infoToSlot)
	{
		var equipment = onPlayer.GetComponent<Equipment>();
		if (equipment == null) return false;
		if (infoToSlot == null) return false;

		if (infoToSlot.SlotIdentifier.SlotIdentifierType != SlotIdentifierType.Named) return false;

		return equipment.GetClothingItem(infoToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)) != null;
	}

	/// <summary>
	/// Server-side method, sets whether this object can be picked up.
	/// </summary>
	[Server]
	public void ServerSetCanPickup(bool canPickup)
	{
		this.canPickup = canPickup;
	}

	#region Interaction

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (UniversalObjectPhysics.IsBuckled) return false;

		if (canPickup == false) return false;
		//we need to be the target
		if (interaction.TargetObject != gameObject) return false;
		//hand needs to be empty for pickup
		if (interaction.HandObject != null) return false;
		//instead of the base logic, we need to use extended range check for CanApply
		if (DefaultWillInteract.Default(interaction, side) == false) return false;
		if (Validations.CanApply(interaction.PerformerPlayerScript, interaction.TargetObject, side, true) == false) return false;

		return true;
	}

	public void ClientPredictInteraction(HandApply interaction)
	{
		if ( interaction.Performer.GetComponent<PlayerScript>().IsGameObjectReachable( this.gameObject, false ))
		{
			//Predictive disappear only if item is within normal range
			gameObject.GetComponent<UniversalObjectPhysics>().DisappearFromWorld();
		}
	}

	public void ServerRollbackClient(HandApply interaction)
	{
		//Rollback prediction (inform player about item's true state)
		GetComponent<UniversalObjectPhysics>().ResetLocationOnClients();
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		StartCoroutine(ServerPerformInteractionLogic(interaction));
	}

	private IEnumerator ServerPerformInteractionLogic(HandApply interaction)
	{
		//we validated, but object may only be in extended range
		var uop = GetComponent<UniversalObjectPhysics>();
		var ps = interaction.Performer.GetComponent<PlayerScript>();
		var extendedRangeOnly = !ps.IsRegisterTileReachable(uop.registerTile, true);

		//Start the animation on the server and clients.
		PickupAnim(interaction.Performer.gameObject);
		RpcPickupAnimation(interaction.Performer.gameObject);
		OnMoveToPlayerInventory?.Invoke(interaction.Performer);
		if (lastTouch != null) lastTouch.LastTouchedBy = interaction.PerformerPlayerScript.PlayerInfo;
		yield return WaitFor.Seconds(pickupAnimSpeed);

		//Make sure that the object is scaled back to it's original size.
		RpcResetPickupAnim();
		LeanTween.scale(gameObject, new Vector3(1, 1), 0.1f);

		//if it's in extended range only, then we will nudge it if it is stationary
		//or pick it up if it is floating.
		if (extendedRangeOnly && !uop.IsCurrentlyFloating)
		{
			//this item is not floating and it was not within standard range but is within extended range,
			//so we will nudge it
			var position = uop.transform.position;
			var worldPosition = position;
			var trajectory = ((Vector3)ps.WorldPos - worldPosition) / Random.Range(10, 31);
			uop.NewtonianPush(trajectory ,2 , spinFactor: 15 );

			Loggy.LogTraceFormat( "Nudging! server pos:{0} player pos:{1}", Category.Movement,
				position, interaction.Performer.transform.position);
			//client prediction doesn't handle nudging, so we need to roll them back
			ServerRollbackClient(interaction);
		}
		else
		{
			//pick it up normally - if it was floating, we will grab it while it's floating
			//set ForceInform to false for simulation
			if (Inventory.ServerAdd(this, interaction.HandSlot))
			{
				Loggy.LogTraceFormat("Pickup success! server pos:{0} player pos:{1} (floating={2})", Category.Movement,
					uop.transform.position, interaction.Performer.transform.position, uop.IsCurrentlyFloating);
			}
			else
			{
				//for some reason pickup failed even after earlier validation, need to rollback client
				ServerRollbackClient(interaction);
			}
		}
	}

	#endregion

	private void PickupAnim(GameObject interactor)
	{
		LeanTween.move(gameObject, interactor.transform, pickupAnimSpeed);
		LeanTween.scale(gameObject, new Vector3(0, 0), pickupAnimSpeed);
	}

	[ClientRpc]
	private void RpcPickupAnimation(GameObject interactor)
	{
		//Can happen if object isnt loaded on client yet, e.g during join
		if (interactor == null) return;

		PickupAnim(interactor);
		if(CustomNetworkManager.IsServer == false) OnMoveToPlayerInventory?.Invoke(interactor);
	}

	[ClientRpc]
	private void RpcResetPickupAnim()
	{
		LeanTween.scale(gameObject, new Vector3(1, 1), 0.1f);
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		if (!canPickup) return null;
		var interaction = HandApply.ByLocalPlayer(gameObject);
		if (interaction.TargetObject != gameObject) return null;
		if (interaction.HandObject != null) return null;
		if (!Validations.CanApply(interaction.PerformerPlayerScript, interaction.TargetObject, NetworkSide.Client, true, ReachRange.Standard)) return null;

		return RightClickableResult.Create()
				.AddElement("PickUp", RightClickInteract);
	}

	private void RightClickInteract()
	{
		//trigger the interaction manually, triggered via the right click menu
		InteractionUtils.RequestInteract(HandApply.ByLocalPlayer(gameObject), this);
	}

	/// <summary>
	///     Making reach check less strict when object is flying, otherwise high ping players can never catch shit!
	/// </summary>
	private static bool CanReachFloating(PlayerScript ps, TransformState state)
	{
		return ps.IsPositionReachable(state.WorldPosition, true) || ps.IsPositionReachable(state.WorldPosition - (Vector3)state.WorldImpulse, true, 1.75f);
	}

	/// <summary>
	///     If a SpriteRenderer.sortingOrder is 0 then there will be difficulty
	///     interacting with the object via the InputTrigger especially when placed on
	///     tables. This method makes sure that it is never 0 on start
	/// </summary>
	private void CheckSpriteOrder()
	{
		SpriteRenderer sR = GetComponentInChildren<SpriteRenderer>();
		if (sR != null)
		{
			if (sR.sortingLayerName == "Items" && sR.sortingOrder == 0)
			{
				sR.sortingOrder = 1;
			}
		}
	}

	/// <summary>
	/// NOTE: Please use Inventory instead for moving inventory around.
	///
	/// Internal lifecycle system use only.
	/// Change the slot this pickupable thinks it is in. Null to make it be in no slot.
	/// </summary>
	public void _SetItemSlot(ItemSlot toSlot)
	{
		this.itemSlot = toSlot;
		if (isServer)
		{
			clientSynchronisedStorageIn = toSlot?.ItemStorage.OrNull()?.gameObject.NetId() ?? NetId.Empty;
		}
	}

	/// <summary>
	/// If this is currently in an item slot linked to the local UI, refreshes that local UI slot to display
	/// the current sprite of the gameobject.
	/// </summary>
	public void RefreshUISlotImage()
	{
		if (itemSlot != null && itemSlot.LocalUISlot != null)
		{
			itemSlot.LocalUISlot.RefreshImage();
		}
	}

	public void SetPlayerItemsSprites(ItemsSprites _ItemsSprites, int _spriteIndex = 0, int _variantIndex = 0)
	{
		if (itemSlot != null)
		{
			var equipment = itemSlot.Player.GetComponent<Equipment>();
			if (equipment == null) return;
			var CT = equipment.GetClothingItem(itemSlot.NamedSlot.Value);
			CT.SetInHand(_ItemsSprites);
		}
	}

	public void SetPalette(List<Color> palette)
	{
		if (itemSlot != null)
		{
			var equipment = itemSlot.Player.GetComponent<Equipment>();
			if (equipment == null) return;
			var CT = equipment.GetClothingItem(itemSlot.NamedSlot.Value);
			CT.spriteHandler.SetPaletteOfCurrentSprite(palette, networked:false);
		}
	}
}

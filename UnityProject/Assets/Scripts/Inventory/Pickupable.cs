using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using Random = UnityEngine.Random;

/// <summary>
/// Main API / component for dealing with inventory. Use this if you want to do something to this object that involves
/// inventory. No longer should use InventoryManager.
/// Allows object to be picked up into inventory and moved to different slots, storage objects, and be dropped.
/// </summary>
public class Pickupable : NetworkBehaviour, IPredictedCheckedInteractable<HandApply>,
	IRightClickable, IServerDespawn, IServerInventoryMove
{
	private CustomNetTransform customNetTransform;
	public CustomNetTransform CustomNetTransform => customNetTransform;
	private ObjectBehaviour objectBehaviour;
	private RegisterTile registerTile;

	//controls whether this can currently be picked up.
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
	/// <summary>
	/// If this item is in a slot linked to a UI slot, returns that UI slot.
	/// </summary>
	public UI_ItemSlot LocalUISlot => itemSlot != null ? ItemSlot.LocalUISlot : null;

	private void Awake()
	{
		this.customNetTransform = GetComponent<CustomNetTransform>();
		this.objectBehaviour = GetComponent<ObjectBehaviour>();
		this.registerTile = GetComponent<RegisterTile>();
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
	}

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
		    HasClothingItem(info.FromPlayer, info.FromSlot))
		{
			//clear previous slot appearance
			PlayerAppearanceMessage.SendToAll(info.FromPlayer.gameObject,
				(int)info.FromSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), null);

			//ask target playerscript to update shown name.
			info.FromPlayer.GetComponent<PlayerScript>().RefreshVisibleName();
		}

		if (info.ToPlayer != null &&
		    HasClothingItem(info.ToPlayer, info.ToSlot))
		{
			//change appearance based on new item
			PlayerAppearanceMessage.SendToAll(info.ToPlayer.gameObject,
				(int)info.ToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none), info.MovedObject.gameObject);

			//ask target playerscript to update shown name.
			info.ToPlayer.GetComponent<PlayerScript>().RefreshVisibleName();
		}
	}

	private bool HasClothingItem(RegisterPlayer onPlayer, ItemSlot infoToSlot)
	{
		var equipment = onPlayer.GetComponent<Equipment>();
		if (equipment == null) return false;
		if (infoToSlot.SlotIdentifier.SlotIdentifierType != SlotIdentifierType.Named) return false;

		return equipment.GetClothingItem(infoToSlot.NamedSlot.GetValueOrDefault(NamedSlot.none)) != null;
	}


	/// <summary>
	/// Server-side method, sets whether this object can be picked up.
	/// </summary>
	/// <param name="canPickup"></param>
	[Server]
	public void ServerSetCanPickup(bool canPickup)
	{
		this.canPickup = canPickup;
	}

	public virtual bool WillInteract(HandApply interaction, NetworkSide side)
	{
		if (!canPickup) return false;
		//we need to be the target
		if (interaction.TargetObject != gameObject) return false;
		//hand needs to be empty for pickup
		if (interaction.HandObject != null) return false;
		//instead of the base logic, we need to use extended range check for CanApply
		if (!Validations.CanApply(interaction, side, true, ReachRange.Standard, isPlayerClick: true)) return false;

		return true;
	}

	public void ClientPredictInteraction(HandApply interaction)
	{
		if ( interaction.Performer.GetComponent<PlayerScript>().IsInReach( this.gameObject, false ))
		{
			//Predictive disappear only if item is within normal range
			gameObject.GetComponent<CustomNetTransform>().DisappearFromWorld();
		}
	}

	public void ServerRollbackClient(HandApply interaction)
	{
		//Rollback prediction (inform player about item's true state)
		GetComponent<CustomNetTransform>().NotifyPlayer(interaction.Performer.GetComponent<NetworkIdentity>().connectionToClient);
	}

	public virtual void ServerPerformInteraction(HandApply interaction)
	{
		//we validated, but object may only be in extended range
		var cnt = GetComponent<CustomNetTransform>();
		var ps = interaction.Performer.GetComponent<PlayerScript>();
		var extendedRangeOnly = !ps.IsInReach(cnt.RegisterTile, true);

		//if it's in extended range only, then we will nudge it if it is stationary
		//or pick it up if it is floating.
		if (extendedRangeOnly && !cnt.IsFloatingServer)
		{
			//this item is not floating and it was not within standard range but is within extended range,
			//so we will nudge it
			var worldPosition = cnt.RegisterTile.WorldPositionServer;
			var trajectory = ((Vector3)ps.WorldPos-worldPosition)/ Random.Range( 10, 31 );
			cnt.Nudge( new NudgeInfo
			{
				OriginPos = worldPosition - trajectory,
				Trajectory = trajectory,
				SpinMode = SpinMode.Clockwise,
				SpinMultiplier = 15,
				InitialSpeed = 2
			} );
			Logger.LogTraceFormat( "Nudging! server pos:{0} player pos:{1}", Category.Security,
				cnt.ServerState.WorldPosition, interaction.Performer.transform.position);
			//client prediction doesn't handle nudging, so we need to roll them back
			ServerRollbackClient(interaction);
		}
		else
		{
			//pick it up normally - if it was floating, we will grab it while it's floating
			//set ForceInform to false for simulation
			if (Inventory.ServerAdd(this, interaction.HandSlot))
			{
				Logger.LogTraceFormat("Pickup success! server pos:{0} player pos:{1} (floating={2})", Category.Security,
					cnt.ServerState.WorldPosition, interaction.Performer.transform.position, cnt.IsFloatingServer);
			}
			else
			{
				//for some reason pickup failed even after earlier validation, need to rollback client
				ServerRollbackClient(interaction);
			}
		}
	}

	public RightClickableResult GenerateRightClickOptions()
	{
		if (!canPickup) return null;
		var interaction = HandApply.ByLocalPlayer(gameObject);
		if (interaction.TargetObject != gameObject) return null;
		if (interaction.HandObject != null) return null;
		if (!Validations.CanApply(interaction, NetworkSide.Client, true, ReachRange.Standard, isPlayerClick: false)) return null;

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
		return ps.IsInReach(state.WorldPosition, true) || ps.IsInReach(state.WorldPosition - (Vector3)state.WorldImpulse, true, 1.75f);
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
	/// <param name="toSlot"></param>
	public void _SetItemSlot(ItemSlot toSlot)
	{
		this.itemSlot = toSlot;
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

	/// <summary>
	/// If this is currently in an item slot linked to the local UI, changes the secondary
	/// sprite of that UI slot to use newSprite.
	/// </summary>
	public void UpdateSecondaryUISlotImage(Sprite newSecondaryImage)
	{
		if (itemSlot != null && itemSlot.LocalUISlot != null)
		{
			itemSlot.LocalUISlot.SetSecondaryImage(newSecondaryImage);
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
			CT.spriteHandler.SetPaletteOfCurrentSprite(palette, Network:false);
		}
	}
}

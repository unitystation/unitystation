
using UnityEngine;

/// <summary>
/// Main API for modifying inventory. If you need to do something with inventory, check here first.
/// </summary>
public static class Inventory
{
	/// <summary>
	/// Inventory move in which the object in one slot is transferred directly to another
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="toSlot"></param>
	/// <returns>true if successful</returns>
	public static bool ServerTransfer(ItemSlot fromSlot, ItemSlot toSlot)
	{
		return ServerPerform(InventoryMove.Transfer(fromSlot, toSlot));
	}

	/// <summary>
	/// Inventory move in which the object was not previously in the inventory system (not in any ItemSlot)
	/// and now is.
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <returns>true if successful</returns>
	public static bool ServerAdd(Pickupable addedObject, ItemSlot toSlot)
	{
		return ServerPerform(InventoryMove.Add(addedObject, toSlot));
	}

	/// <summary>
	/// Inventory move in which the object was not previously in the inventory system (not in any ItemSlot)
	/// and now is.
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <returns>true if successful</returns>
	public static bool ServerAdd(GameObject addedObject, ItemSlot toSlot)
	{
		return ServerPerform(InventoryMove.Add(addedObject, toSlot));
	}

	/// <summary>
	/// Inventory move in which the object in the slot is despawned directly from inventory and doesn't reappear
	/// in the world.
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <returns>true if successful</returns>
	public static bool ServerDespawn(ItemSlot fromSlot)
	{
		return ServerPerform(InventoryMove.Despawn(fromSlot));
	}

	/// <summary>
	/// Inventory move in which the object in the slot is dropped into the world at the location of its root storage
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="targetWorldPosition">world position to drop at, leave null to drop at holder's position</param>
	/// <returns>true if successful</returns>
	public static bool ServerDrop(ItemSlot fromSlot, Vector2? targetWorldPosition = null)
	{
		return ServerPerform(InventoryMove.Drop(fromSlot, targetWorldPosition));
	}

	/// <summary>
	/// NOTE: This should RARELY be used, and this method may even be removed later!
	/// It's only here as a last resort / stopgap in case you can't figure out
	/// a better alternative. If you need to store an object for your component, use an ItemStorage
	/// on the object your component is on and transfer into it. It's bad to have items just hanging out at hidden pos.
	///
	/// Inventory move in which the object in the slot is removed from inventory but neither dropped in the world,
	/// despawned, or made visible. Instead it is kept invisible at hiddenpos.
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <returns>true if successful</returns>
	public static bool ServerVanish(ItemSlot fromSlot)
	{
		return ServerPerform(InventoryMove.Vanish(fromSlot));
	}

	/// <summary>
	/// Inventory move in which the object in the slot is thrown into the world from the location of its root storage
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="targetWorldPosition">world position being targeted by the throw</param>
	/// <param name="spinMode"></param>
	/// <param name="aim">body part to target</param>
	/// <returns>true if successful</returns>
	public static bool ServerThrow(ItemSlot fromSlot, Vector2 targetWorldPosition, SpinMode spinMode = SpinMode.CounterClockwise, BodyPartType aim = BodyPartType.Chest)
	{
		return ServerPerform(InventoryMove.Throw(fromSlot, targetWorldPosition, spinMode, aim));
	}

	/// <summary>
	/// Server-side only. General purpose method for performing an inventory move. Performs the move indicated by toPerform.
	/// </summary>
	/// <param name="toPerform"></param>
	/// <returns>true if successful</returns>
	public static bool ServerPerform(InventoryMove toPerform)
	{
		var pickupable = toPerform.MovedObject;
		if (pickupable == null)
		{
			Logger.LogError("Inventory move attempted with null object. Please ensure" +
			                      " toPerform.MovedObject is not null. This could indicate that a move was" +
			                      " attempted on a slot that has no item. Move will not be performed", Category.Inventory);
			return false;
		}

		//figure out which kind of move to do
		if (toPerform.InventoryMoveType == InventoryMoveType.Add)
		{
			if (!ServerPerformAdd(toPerform, pickupable)) return false;
		}
		else if (toPerform.InventoryMoveType == InventoryMoveType.Remove)
		{
			if (!ServerPerformRemove(toPerform, pickupable)) return false;
		}
		else if (toPerform.InventoryMoveType == InventoryMoveType.Transfer)
		{
			if (!ServerPerformTransfer(toPerform, pickupable)) return false;
		}
		else
		{
			Logger.LogErrorFormat("Unrecognized move type {0}. Please add logic to this method to support this move type.",
				Category.Inventory, toPerform.InventoryMoveType);
		}

		return true;
	}

	private static bool ServerPerformTransfer(InventoryMove toPerform, Pickupable pickupable)
	{
		//transfer from one slot to another
		var toSlot = toPerform.ToSlot;
		if (toSlot == null)
		{
			Logger.LogErrorFormat("Attempted to transfer {0} to another slot but target slot was null." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (toSlot.Item != null)
		{
			Logger.LogErrorFormat(
				"Attempted to transfer {0} to target slot but target slot {1} already had something in it." +
				" Move will not be performed.", Category.Inventory, pickupable.name, toSlot);
			return false;
		}

		if (pickupable.ItemSlot == null)
		{
			Logger.LogErrorFormat("Attempted to transfer {0} to target slot but item is not in a slot." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		var fromSlot = toPerform.FromSlot;
		if (fromSlot == null)
		{
			Logger.LogErrorFormat("Attempted to transfer {0} to target slot but from slot was null." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (!Validations.CanFit(toSlot, pickupable, NetworkSide.Server, true))
		{
			Logger.LogErrorFormat("Attempted to transfer {0} to slot {1} but slot cannot fit this item." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name, toSlot);
			return false;
		}

		pickupable.ServerSetItemSlot(null);
		fromSlot.ServerRemoveItem();

		pickupable.ServerSetItemSlot(toSlot);
		toSlot.ServerSetItem(pickupable);

		foreach (var onMove in pickupable.GetComponents<IServerOnInventoryMove>())
		{
			onMove.ServerOnInventoryMove(toPerform);
		}

		return true;
	}

	private static bool ServerPerformRemove(InventoryMove toPerform, Pickupable pickupable)
	{
		if (pickupable.ItemSlot == null)
		{
			Logger.LogErrorFormat("Attempted to remove {0} from inventory but item is not in a slot." +
			                      " remove will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		var fromSlot = toPerform.FromSlot;
		if (fromSlot == null)
		{
			Logger.LogErrorFormat("Attempted to remove {0} from inventory but from slot was null." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (fromSlot.Item != null)
		{
			Logger.LogWarningFormat("Attempted to remove {0} from inventory but from slot {1} had no item in it." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, fromSlot);
			return false;
		}

		//update pickupable's item and slot's item
		pickupable.ServerSetItemSlot(null);
		fromSlot.ServerRemoveItem();

		//decide how it should be removed
		var removeType = toPerform.RemoveType;
		if (removeType == InventoryRemoveType.Despawn)
		{
			//destroy
			PoolManager.PoolNetworkDestroy(pickupable.gameObject);
		}
		else if (removeType == InventoryRemoveType.Drop)
		{
			//drop where it is
			//determine where it will appear
			var holder = fromSlot.GetRootStorage();

			if (holder.GetComponent<ObjectBehaviour>().parentContainer != null)
			{
				//TODO: Should we support dropping things while in a PushPull container?
				Logger.LogWarningFormat(
					"Trying to drop an item from slot {1} while in a PushPull container {1} is not currently supported, dropping will" +
					" not occur.", Category.Inventory, fromSlot,
					holder.GetComponent<ObjectBehaviour>().parentContainer.name);
				return false;
			}

			var holderPlayer = holder.GetComponent<PlayerSync>();
			var cnt = pickupable.GetComponent<CustomNetTransform>();
			Vector3 targetWorldPos = toPerform.TargetWorldPos.GetValueOrDefault(holder.gameObject.TileWorldPosition());
			if (holderPlayer != null)
			{
				//dropping from player
				//Inertia drop works only if player has external impulse (space floating etc.)
				cnt.InertiaDrop(targetWorldPos, holderPlayer.SpeedServer,
					holderPlayer.ServerImpulse);
			}
			else
			{
				//dropping from not-held storage
				cnt.AppearAtPositionServer(targetWorldPos);
			}
		}
		else if (removeType == InventoryRemoveType.Throw)
		{
			//throw / eject
			//determine where it will be thrown from
			var holder = fromSlot.GetRootStorage();

			if (holder.GetComponent<ObjectBehaviour>().parentContainer != null)
			{
				//TODO: Should we support dropping things while in a PushPull container?
				Logger.LogWarningFormat(
					"Trying to throw an item from slot {1} while in a PushPull container {1} is not currently supported, dropping will" +
					" not occur.", Category.Inventory, fromSlot,
					holder.GetComponent<ObjectBehaviour>().parentContainer.name);
				return false;
			}

			var cnt = pickupable.GetComponent<CustomNetTransform>();
			var throwInfo = new ThrowInfo
			{
				ThrownBy = holder.gameObject,
				Aim = toPerform.ThrowAim.GetValueOrDefault(BodyPartType.Chest),
				OriginPos = holder.gameObject.TileWorldPosition().To3Int(),
				TargetPos = (Vector3) toPerform.TargetWorldPos,
				SpinMode = toPerform.ThrowSpinMode.GetValueOrDefault(SpinMode.Clockwise)
			};
			//dropping from player
			//Inertia drop works only if player has external impulse (space floating etc.)
			cnt.Throw(throwInfo);

			//Simplified counter-impulse for players in space
			var ps = holder.GetComponent<PlayerSync>();
			if (ps != null && ps.IsWeightlessServer)
			{
				ps.Push(Vector2Int.RoundToInt(-throwInfo.Trajectory.normalized));
			}
		}
		//NOTE: vanish doesn't require any extra logic. The item is already at hiddenpos and has
		//already been removed from the inventory system.

		foreach (var onMove in pickupable.GetComponents<IServerOnInventoryMove>())
		{
			onMove.ServerOnInventoryMove(toPerform);
		}

		return true;
	}

	private static bool ServerPerformAdd(InventoryMove toPerform, Pickupable pickupable)
	{
		//item is not currently in inventory, it should be moved into inventory system into
		//the indicated slot.

		if (pickupable.ItemSlot != null)
		{
			Logger.LogErrorFormat("Attempted to add {0} to inventory but item is already in slot {1}." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, pickupable.ItemSlot);
			return false;
		}

		var toSlot = toPerform.ToSlot;
		if (toSlot == null)
		{
			Logger.LogErrorFormat("Attempted to add {0} to inventory but target slot was null." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (toSlot.Item != null)
		{
			Logger.LogErrorFormat("Attempted to add {0} to inventory but target slot {1} already had something in it." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, toSlot);
			return false;
		}

		if (!Validations.CanFit(toSlot, pickupable, NetworkSide.Server, true))
		{
			Logger.LogErrorFormat("Attempted to add {0} to slot {1} but slot cannot fit this item." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name, toSlot);
			return false;
		}

		//go poof, it's in inventory now.
		pickupable.GetComponent<CustomNetTransform>().DisappearFromWorldServer();

		//no longer inside any PushPull
		pickupable.GetComponent<ObjectBehaviour>().parentContainer = null;
		pickupable.GetComponent<RegisterTile>().UpdatePositionServer();

		//update pickupable's item and slot's item
		pickupable.ServerSetItemSlot(toSlot);
		toSlot.ServerSetItem(pickupable);

		foreach (var onMove in pickupable.GetComponents<IServerOnInventoryMove>())
		{
			onMove.ServerOnInventoryMove(toPerform);
		}

		return true;
	}

	//TODO: Refactor everything below, taken from InventoryManager

//	public static void ClientEquipInInvSlot(PlayerNetworkActions pna, GameObject item, EquipSlot equipSlot)
//	{
//		var inventorySlot = pna.Inventory[equipSlot];
//		inventorySlot.Item = item;
//		var UIitemSlot = InventorySlotCache.GetSlotByEvent(inventorySlot.equipSlot);
//		UIitemSlot.SetItem(item);
//	}
//
//	private static bool IsEquipSpriteSlot(EquipSlot equipSlot)
//	{
//		if (equipSlot == EquipSlot.id || equipSlot == EquipSlot.storage01 ||
//			equipSlot == EquipSlot.storage02 || equipSlot == EquipSlot.suitStorage)
//		{
//			return false;
//		}
//		return true;
//	}

	//Server only:
	/// <summary>
	/// Get an Inventory slot from originators hand id (i.e leftHand)
	/// Can only be used ont he server
	/// </summary>
	/// <param name="originator"></param>
	/// <param name="hand"></param>
	/// <returns></returns>
//	public static InventorySlot GetSlotFromOriginatorHand(GameObject originator, EquipSlot hand)
//	{
//		var pna = originator.GetComponent<PlayerNetworkActions>();
//		var slot = pna.Inventory[hand];
//		return slot;
//	}
}

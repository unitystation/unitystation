
using UnityEngine;

/// <summary>
/// Describes (but does not perform) a particular inventory movement of a particular Pickupable game object.
/// The object is being moved within, out of, or into the inventory system.
/// This is used to perform the described movement (in Inventory) as well as pass the information to
/// inventory movement hook interface implementers
/// </summary>
public class InventoryMove
{
	/// <summary>
	/// What kind of movement occurred
	/// </summary>
	public readonly InventoryMoveType InventoryMoveType;

	/// <summary>
	/// Pickupable game object that is being moved
	/// </summary>
	public readonly Pickupable MovedObject;

	/// <summary>
	/// Slot the MovedObject was moved from (null if InventoryMove.Add)
	/// </summary>
	public readonly ItemSlot FromSlot;

	/// <summary>
	/// Slot the MovedObject was moved to (null if InventoryMove.Remove)
	/// </summary>
	public readonly ItemSlot ToSlot;

	/// <summary>
	/// If InventoryMove.Remove, the way in which it is removed.
	/// </summary>
	public readonly InventoryRemoveType? RemoveType;

	/// <summary>
	/// If InventoryRemoveType.Throw, what body part to aim at
	/// </summary>
	public readonly BodyPartType? ThrowAim;

	/// <summary>
	/// If InventoryRemoveType.Throw or Drop, what world position is being targeted by the throw / drop. If null
	/// and its a drop, it will drop at holder position.
	/// </summary>
	public readonly Vector2? TargetWorldPos;

	/// <summary>
	/// If InventoryRemoveType.Throw, what spin mode to use for the object
	/// </summary>
	public readonly SpinMode? ThrowSpinMode;

	/// <summary>
	/// If fromSlot is a player's top-level inventory, returns that player. Otherwise null.
	/// </summary>
	public RegisterPlayer FromPlayer => FromSlot?.Player;

	/// <summary>
	/// If toslot is a player's top-level inventory, returns that player. Otherwise null.
	/// </summary>
	public RegisterPlayer ToPlayer => ToSlot?.Player;

	/// <summary>
	/// If fromSlot is in a player's slot tree (i.e. anywhere in their
	/// inventory, even in a bag), returns that player. Otherwise null.
	/// </summary>
	public RegisterPlayer FromRootPlayer => FromSlot?.RootPlayer();

	/// <summary>
	/// If toslot is in a player's slot tree (i.e. anywhere in their
	/// inventory, even in a bag), returns that player. Otherwise null.
	/// </summary>
	public RegisterPlayer ToRootPlayer => ToSlot?.RootPlayer();


	public InventoryMove(InventoryMoveType inventoryMoveType, Pickupable movedObject, ItemSlot fromSlot, ItemSlot slot,
		InventoryRemoveType? removeType = null, BodyPartType? throwAim = null, Vector2? targetWorldPos = null, SpinMode? throwSpinMode = null)
	{
		InventoryMoveType = inventoryMoveType;
		MovedObject = movedObject;
		FromSlot = fromSlot;
		ToSlot = slot;
		RemoveType = removeType;
		ThrowAim = throwAim;
		TargetWorldPos = targetWorldPos;
		ThrowSpinMode = throwSpinMode;
	}

	/// <summary>
	/// Inventory move in which the object in one slot is transferred directly to another
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="toSlot"></param>
	/// <returns></returns>
	public static InventoryMove Transfer(ItemSlot fromSlot, ItemSlot toSlot)
	{
		return new InventoryMove(InventoryMoveType.Transfer, fromSlot.Item, fromSlot, toSlot);
	}

	/// <summary>
	/// Inventory move in which the object was not previously in the inventory system (not in any ItemSlot)
	/// and now is.
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <returns></returns>
	public static InventoryMove Add(Pickupable addedObject, ItemSlot toSlot)
	{
		return new InventoryMove(InventoryMoveType.Add, addedObject, null, toSlot);
	}

	/// <summary>
	/// Inventory move in which the object was not previously in the inventory system (not in any ItemSlot)
	/// and now is.
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <returns></returns>
	public static InventoryMove Add(GameObject addedObject, ItemSlot toSlot)
	{
		var pu = addedObject.GetComponent<Pickupable>();
		if (pu == null)
		{
			Logger.LogErrorFormat("{0} has no pickupable, thus cannot be added to inventory", Category.Inventory, addedObject);
		}
		return new InventoryMove(InventoryMoveType.Add, pu, null, toSlot);
	}

	/// <summary>
	/// Inventory move in which the object in the slot is despawned directly from inventory and doesn't reappear
	/// in the world.
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <returns></returns>
	public static InventoryMove Despawn(ItemSlot fromSlot)
	{
		return new InventoryMove(InventoryMoveType.Remove, fromSlot.Item, fromSlot, null, InventoryRemoveType.Despawn);
	}

	/// <summary>
	/// Inventory move in which the object in the slot is dropped into the world at the location of its root storage
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="targetWorldPosition">world position to drop at, leave null to drop at holder's position</param>
	/// <returns></returns>
	public static InventoryMove Drop(ItemSlot fromSlot, Vector2? targetWorldPosition = null)
	{
		return new InventoryMove(InventoryMoveType.Remove, fromSlot.Item, fromSlot, null, InventoryRemoveType.Drop, null, targetWorldPosition);
	}

	/// <summary>
	/// NOTE: This should RARELY be used! If you need to store an object for your component, use an ItemStorage
	/// on that component and transfer into it.
	///
	/// Inventory move in which the object in the slot is removed from inventory but neither dropped in the world,
	/// despawned, or made visible. Instead it is kept invisible at hiddenpos.
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <returns></returns>
	public static InventoryMove Vanish(ItemSlot fromSlot)
	{
		return new InventoryMove(InventoryMoveType.Remove, fromSlot.Item, fromSlot, null, InventoryRemoveType.Vanish);
	}

	/// <summary>
	/// Inventory move in which the object in the slot is thrown into the world from the location of its root storage
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="targetWorldPosition">world position being targeted by the throw</param>
	/// <param name="spinMode"></param>
	/// <param name="aim">body part to target</param>
	/// <returns></returns>
	public static InventoryMove Throw(ItemSlot fromSlot, Vector2 targetWorldPosition, SpinMode spinMode = SpinMode.CounterClockwise, BodyPartType aim = BodyPartType.Chest)
	{
		return new InventoryMove(InventoryMoveType.Remove, fromSlot.Item, fromSlot, null, InventoryRemoveType.Throw, aim, targetWorldPosition, spinMode);
	}
}

/// <summary>
/// The different kinds of inventory movements
/// </summary>
public enum InventoryMoveType
{
	/// <summary>
	/// Transferred directly from one item slot to another, which may be between bags, players,
	/// player to bag, etc...
	/// </summary>
	Transfer,
	/// <summary>
	/// Totally removed from the inventory system, such that it now does not
	/// exist in any ItemSlot. For example, being dropped on the ground.
	/// </summary>
	Remove,
	/// <summary>
	/// Added to the inventory system when it previously was not in any ItemSlot.
	/// For example, being picked up from the ground or directly spawned into a slot.
	/// </summary>
	Add
}

/// <summary>
/// Different ways in which inventory can be removed
/// </summary>
public enum InventoryRemoveType
{
	/// <summary>
	/// Despawned directly from inventory, doesn't go into the world first.
	/// </summary>
	Despawn,
	/// <summary>
	/// Dropped into the world wherever its root storage is
	/// </summary>
	Drop,
	/// <summary>
	/// Thrown / ejected into the world in a particular direction from wherever its root storage is
	/// </summary>
	Throw,
	/// <summary>
	/// Removed from inventory, but not added to the world or despawned - simply kept invisible at hiddenpos.
	/// Should rarely be used.
	/// </summary>
	Vanish
}

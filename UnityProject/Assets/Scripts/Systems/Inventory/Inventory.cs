using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Items;
using Logs;
using Messages.Client;
using UnityEngine;
using Systems.Storage;
using Objects;
using Random = UnityEngine.Random;

/// <summary>
/// Main API for modifying inventory. If you need to do something with inventory, check here first.
/// </summary>
public static class Inventory
{
	/// <summary>
	/// Instantiates prefab then add it to inventory
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <param name="replacementStrategy">what to do if toSlot is already occupied</param>
	/// <returns>true if successful</returns>
	public static bool ServerSpawnPrefab(GameObject addedObject, ItemSlot toSlot, ReplacementStrategy replacementStrategy = ReplacementStrategy.Cancel, bool IgnoreRestraints = false)
	{
		var spawn = Spawn.ServerPrefab(addedObject);
		return ServerPerform(InventoryMove.Add(spawn.GameObject.GetComponent<Pickupable>(), toSlot, replacementStrategy, IgnoreRestraints));
	}

	/// <summary>
	/// Inventory move in which the object in one slot is transferred directly to another
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="toSlot"></param>
	/// <param name="replacementStrategy">what to do if toSlot is already occupied</param>
	/// <returns>true if successful</returns>
	public static bool ServerTransfer(ItemSlot fromSlot, ItemSlot toSlot, ReplacementStrategy replacementStrategy = ReplacementStrategy.Cancel, bool IgnoreRestraints = false)
	{
		return ServerPerform(InventoryMove.Transfer(fromSlot, toSlot, replacementStrategy, IgnoreRestraints));
	}

	/// <summary>
	/// Inventory move in which the object was not previously in the inventory system (not in any ItemSlot)
	/// and now is.
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <param name="replacementStrategy">what to do if toSlot is already occupied</param>
	/// <returns>true if successful</returns>
	public static bool ServerAdd(Pickupable addedObject, ItemSlot toSlot, ReplacementStrategy replacementStrategy = ReplacementStrategy.Cancel, bool IgnoreRestraints = false)
	{
		return ServerPerform(InventoryMove.Add(addedObject, toSlot, replacementStrategy,IgnoreRestraints));
	}

	/// <summary>
	/// Inventory move in which the object was not previously in the inventory system (not in any ItemSlot)
	/// and now is.
	/// </summary>
	/// <param name="addedObject"></param>
	/// <param name="toSlot"></param>
	/// <param name="replacementStrategy">what to do if toSlot is already occupied</param>
	/// <returns>true if successful</returns>
	public static bool ServerAdd(GameObject addedObject, ItemSlot toSlot, ReplacementStrategy replacementStrategy = ReplacementStrategy.Cancel, bool IgnoreRestraints = false)
	{
		return ServerPerform(InventoryMove.Add(addedObject, toSlot, replacementStrategy, IgnoreRestraints));
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
	/// Consume the indicated amount of the object in the slot if it is stackable, otherwise despawn it.
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <returns>true if successful</returns>
	public static bool ServerConsume(ItemSlot fromSlot, int amountToConsume)
	{
		if (fromSlot.ItemObject == null) return false;
		var stackable = fromSlot.ItemObject.GetComponent<Stackable>();
		if (stackable != null)
		{
			return stackable.ServerConsume(amountToConsume);
		}
		else
		{
			return ServerPerform(InventoryMove.Despawn(fromSlot));
		}
	}

	/// <summary>
	/// Inventory move in which the object (assumed to be in a slot) is despawned directly from inventory and doesn't reappear
	/// in the world.
	/// </summary>
	/// <param name="objectInSlot">object to despawn from inventory. Will be despawned normally if not in slot.</param>
	/// <returns>true if successful</returns>
	public static async Task<bool> ServerDespawn(GameObject objectInSlot)
	{
		var pu = objectInSlot.GetComponent<Pickupable>();
		if (pu == null || pu.ItemSlot == null)
		{
			var result = await Despawn.ServerSingle(objectInSlot);

			return result.Successful;
		}

		return ServerDespawn(pu.ItemSlot);
	}

	/// <summary>
	/// Inventory move in which the object in the slot is dropped into the world at the location of its root storage
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="worldTargetVector">world space vector pointing from origin to targeted position to throw, leave null
	/// to drop at holder's position</param>
	/// <returns>true if successful</returns>
	public static bool ServerDrop(ItemSlot fromSlot, Vector2? worldTargetVector = null)
	{
		return ServerPerform(InventoryMove.Drop(fromSlot, worldTargetVector));
	}

	/// <summary>
	/// If you're too lazy to get the ItemSlot This will do it for you or Return false if it can't find ItemSlot it is in
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="worldTargetVector"></param>
	/// <returns></returns>
	public static bool ServerDrop(GameObject Object, Vector2? worldTargetVector = null)
	{
		if (Object.TryGetComponent<Pickupable>(out var Pickupable) == false) return false;
		if (Pickupable.ItemSlot == null) return false;

		return ServerPerform(InventoryMove.Drop(Pickupable.ItemSlot , worldTargetVector));
	}


	/// <summary>
	/// Drops all the items in the slots of the player
	/// </summary>
	/// <param name="playerStorage">players dynamic storage</param>
	/// <param name="worldTargetVector">world space vector pointing from origin to targeted position to throw, leave null
	/// to drop at holder's position</param>
	/// <returns>true if successful</returns>
	public static void ServerDropAll(DynamicItemStorage playerStorage, Vector2? worldTargetVector = null)
	{
		var playerItems = playerStorage.GetItemSlots().ToList();

		foreach (var playerItemSlot in playerItems.NotNull())
		{
			if(playerItemSlot.IsEmpty) continue;
			ServerPerform(InventoryMove.Drop(playerItemSlot, worldTargetVector));
		}
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
	/// Same as above but can support stackables (playing item one at a time into a machine)
	/// returns the object that was removed from the stack also
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <returns></returns>
	public static GameObject ServerVanishStackable(ItemSlot fromSlot)
	{
		if (fromSlot.ItemObject == null) return null;
		var stackable = fromSlot.ItemObject.GetComponent<Stackable>();
		if (stackable != null)
		{
			var clone = stackable.ServerRemoveOne();
			ServerPerform(new InventoryMove(InventoryMoveType.Remove, clone.GetComponent<Pickupable>(), fromSlot, null, InventoryRemoveType.Vanish));
			return clone;
		}
		else
		{
			ServerPerform(InventoryMove.Vanish(fromSlot));
			return fromSlot.Item.gameObject;
		}
	}

	/// <summary>
	/// Inventory move in which the object in the slot is thrown into the world from the location of its root storage
	/// </summary>
	/// <param name="fromSlot"></param>
	/// <param name="worldTargetVector">world space vector pointing from origin to targeted position to throw</param>
	/// <param name="spinMode"></param>
	/// <param name="aim">body part to target</param>
	/// <returns>true if successful</returns>
	public static bool ServerThrow(ItemSlot fromSlot, Vector2 worldTargetVector, BodyPartType aim = BodyPartType.Chest)
	{
		return ServerPerform(InventoryMove.Throw(fromSlot, worldTargetVector, aim));
	}

	/// <summary>
	/// Server-side only. General purpose method for performing an inventory move. Performs the move indicated by toPerform.
	/// </summary>
	/// <param name="toPerform"></param>
	/// <returns>true if successful</returns>
	public static bool ServerPerform(InventoryMove toPerform)
	{
		if (CustomNetworkManager.IsServer == false)
		{
			Loggy.LogError("Tried to manipulate item Storage While being on the client");
			return false;
		}

		if (toPerform == null)
		{
			Loggy.LogError("Inventory move null, likely it failed due to previous error.", Category.Inventory);
			return false;
		}
		var pickupable = toPerform.MovedObject;
		if (pickupable == null)
		{
			Loggy.LogTrace("Inventory move attempted with null object. Move will not be performed", Category.Inventory);
			return false;
		}

		if (toPerform.FromSlot != null && toPerform.FromSlot.Invalid)
		{
			Loggy.LogErrorFormat("Inventory move attempted with invalid slot {0}. This slot reference should've" +
			                      " been cleaned up when the round restarted yet somehow didn't.", Category.Inventory,
				toPerform.FromSlot);
			return false;
		}
		if (toPerform.ToSlot != null && toPerform.ToSlot.Invalid)
		{
			Loggy.LogErrorFormat("Inventory move attempted with invalid slot {0}. This slot reference should've" +
			                      " been cleaned up when the round restarted yet somehow didn't.", Category.Inventory,
				toPerform.ToSlot);
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
			Loggy.LogTraceFormat("Unrecognized move type {0}. Please add logic to this method to support this move type.",
				Category.Inventory, toPerform.InventoryMoveType);
		}

		PlayInventorySound(toPerform.FromSlot, toPerform.MovedObject.gameObject.Item());

		return true;
	}

	private static void PlayInventorySound(ItemSlot slot, Items.ItemAttributesV2 item)
	{
		if (slot == null || item == null) return;

		if (item.InventoryRemoveSound != null)
		{
			_ = SoundManager.PlayNetworkedAtPosAsync(item.InventoryRemoveSound, slot.ItemStorage.gameObject.AssumedWorldPosServer());
		}
	}

	private static bool ServerPerformTransfer(InventoryMove toPerform, Pickupable pickupable)
	{
		//transfer from one slot to another
		var toSlot = toPerform.ToSlot;
		var fromSlot = toPerform.FromSlot;
		if (toSlot == null)
		{
			Loggy.LogTraceFormat("Attempted to transfer {0} to another slot but target slot was null." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (toSlot.Item != null)
		{

			if (toSlot.ItemNotRemovable)
			{
				Loggy.LogTraceFormat("Attempted to remove {0} from inventory but from slot {1} had ItemNotRemovable." +
				                      " Move will not be performed.", Category.Inventory, pickupable.name, fromSlot);
				return false;
			}

			// Check if the items can be stacked
			var stackableTarget = toSlot.Item.GetComponent<Stackable>();
			if (stackableTarget != null && stackableTarget.CanAccommodate(pickupable.gameObject))
			{
				toSlot.Item.GetComponent<Stackable>().ServerCombine(pickupable.GetComponent<Stackable>());
				return true;
			}

			switch (toPerform.ReplacementStrategy)
			{
				case ReplacementStrategy.DespawnOther:
					Loggy.LogTraceFormat("Attempted to transfer from slot {0} to slot {1} which already had something in it." +
											" Item in slot will be despawned first.", Category.Inventory, fromSlot, toSlot);
					ServerDespawn(toSlot);
					break;
				case ReplacementStrategy.DropOther:
					Loggy.LogTraceFormat("Attempted to transfer from slot {0} to slot {1} which already had something in it." +
											" Item in slot will be dropped first.", Category.Inventory, fromSlot, toSlot);
					ServerDrop(toSlot);
					break;
				case ReplacementStrategy.Cancel:
				default:
					Loggy.LogTraceFormat("Attempted to transfer from slot {0} to slot {1} which already had something in it." +
											" Transfer will not be performed.", Category.Inventory, fromSlot, toSlot);
					return false;
			}
		}

		if (pickupable.ItemSlot == null)
		{
			Loggy.LogTraceFormat("Attempted to transfer {0} to target slot but item is not in a slot." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}


		if (fromSlot == null)
		{
			Loggy.LogTraceFormat("Attempted to transfer {0} to target slot but from slot was null." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (!Validations.CanFit(toSlot, pickupable, NetworkSide.Server, true) && toPerform.IgnoreConstraints == false)
		{
			Loggy.LogTraceFormat("Attempted to transfer {0} to slot {1} but slot cannot fit this item." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name, toSlot);
			return false;
		}

		if (fromSlot.ItemNotRemovable)
		{
			Loggy.LogTraceFormat("Attempted to remove {0} from inventory but from slot {1} had ItemNotRemovable." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, fromSlot);
			return false;
		}

		pickupable._SetItemSlot(null);
		fromSlot._ServerRemoveItem();

		pickupable._SetItemSlot(toSlot);
		toSlot._ServerSetItem(pickupable);

		foreach (var onMove in pickupable.GetComponents<IServerInventoryMove>())
		{
			onMove.OnInventoryMoveServer(toPerform);
		}

		return true;
	}

	private static bool ServerPerformRemove(InventoryMove toPerform, Pickupable pickupable)
	{
		if (pickupable.ItemSlot == null)
		{
			Loggy.LogTraceFormat("Attempted to remove {0} from inventory but item is not in a slot." +
			                      " remove will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		var fromSlot = toPerform.FromSlot;
		if (fromSlot == null)
		{
			Loggy.LogTraceFormat("Attempted to remove {0} from inventory but from slot was null." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (fromSlot.Item == null)
		{
			Loggy.LogTraceFormat("Attempted to remove {0} from inventory but from slot {1} had no item in it." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, fromSlot);
			return false;
		}


		if (fromSlot.ItemNotRemovable)
		{
			Loggy.LogTraceFormat("Attempted to remove {0} from inventory but from slot {1} had ItemNotRemovable." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, fromSlot);
			return false;
		}

		//update pickupable's item and slot's item
		pickupable._SetItemSlot(null);
		fromSlot._ServerRemoveItem();

		//decide how it should be removed
		var removeType = toPerform.RemoveType;
		var holder = fromSlot.GetRootStorageOrPlayer();
		var universalObjectPhysics = holder?.GetComponent<UniversalObjectPhysics>();
		var parentContainer = universalObjectPhysics == null ? null : universalObjectPhysics.ContainedInObjectContainer;
		if (parentContainer != null && removeType == InventoryRemoveType.Throw)
		{
			Loggy.LogTraceFormat("throwing from slot {0} while in container {1}. Will drop instead.", Category.Inventory,
				fromSlot,
				parentContainer.name);
			removeType = InventoryRemoveType.Drop;
		}

		if (removeType == InventoryRemoveType.Despawn)
		{
			// destroy (safe to skip invnetory despawn check because we already performed necessary inventory logic)
			_ = Despawn.ServerSingle(pickupable.gameObject, true);
		}
		else if (removeType == InventoryRemoveType.Drop)
		{
			// drop where it is
			// determine where it will appear
			if (parentContainer != null)
			{
				Loggy.LogTraceFormat("Dropping from slot {0} while in container {1}", Category.Inventory,
					fromSlot,
					parentContainer.name);
				var objectContainer = parentContainer.GetComponent<ObjectContainer>();
				if (objectContainer == null)
				{
					Loggy.LogWarningFormat("Dropping from slot {0} while in container {1}, but container type was not recognized. " +
					                      "Currently only ObjectContainer is supported. Please add code to handle this case.", Category.Inventory,
						fromSlot,
						universalObjectPhysics.ContainedInObjectContainer.name);
					return false;
				}
				//vanish it and set its parent container
				ServerVanish(fromSlot);
				objectContainer.StoreObject(pickupable.gameObject);
				return true;
			}

			var holderPlayer = holder.OrNull()?.GetComponent<UniversalObjectPhysics>();
			var uop = pickupable.GetComponent<UniversalObjectPhysics>();
			var holderPosition = holder?.gameObject.AssumedWorldPosServer();
			Vector3 targetWorldPos = holderPosition.GetValueOrDefault(Vector3.zero) + (Vector3)toPerform.WorldTargetVector.GetValueOrDefault(Vector2.zero);
			if (holderPlayer != null && toPerform.WorldTargetVector.GetValueOrDefault(Vector2.zero).magnitude == 0)
			{
				// dropping from player
				uop.DropAtAndInheritMomentum(holderPlayer);
			}
			else
			{
				// dropping from not-held storage
				uop.AppearAtWorldPositionServer(targetWorldPos);
			}
		}
		else if (removeType == InventoryRemoveType.Throw)
		{
			// throw / eject
			// determine where it will be thrown from
			var UOP = pickupable.GetComponent<UniversalObjectPhysics>();

			var WorldTrajectory = toPerform.WorldTargetVector.GetValueOrDefault(Vector2.zero).normalized.To3();
			// dropping from player
			// Inertia drop works only if player has external impulse (space floating etc.)
			UOP.DropAtAndInheritMomentum(universalObjectPhysics);


			var Distance = toPerform.WorldTargetVector.Value.magnitude;
			var IA2 = ((ItemAttributesV2) UOP.attributes.Component);
			if (Distance > IA2.ThrowRange)
			{
				Distance = IA2.ThrowRange;
			}

			//v = u + at
			// u – initial velocity
			// v – final velocity
			// a – acceleration
			// t – time
			// s – displacement

			//so
			//0 = IA2.ThrowSpeed + UniversalObjectPhysics.DEFAULT_Friction * t?

			//t = (IA2.ThrowSpeed) / UniversalObjectPhysics.DEFAULT_Friction
			//s=1/2*(u+v)*t

			//s=1/2*(IA2.ThrowSpeed)*(IA2.ThrowSpeed / UniversalObjectPhysics.DEFAULT_Friction)

			//s=1/2*(u+v)*(u/f)

			//s= u^2 / 2f

			//Distance / IA2.ThrowSpeed

			//   (u^2 / 2f) / A2.ThrowSpeed


			// (Mathf.Pow(IA2.ThrowSpeed,2) / 2*UniversalObjectPhysics.DEFAULT_Friction) / A2.ThrowSpeed

			//speedloss  / friction
			UOP.NewtonianPush( WorldTrajectory,((ItemAttributesV2) UOP.attributes.Component).ThrowSpeed
				, (Distance / IA2.ThrowSpeed  ) - ((Mathf.Pow(IA2.ThrowSpeed, 2) / (2*UniversalObjectPhysics.DEFAULT_Friction)) / IA2.ThrowSpeed)
				 , Single.NaN, toPerform.ThrowAim.GetValueOrDefault(BodyPartType.Chest), holder.gameObject, Random.Range(25, 150));


			//
			// Counter-impulse for players in space
			universalObjectPhysics.NewtonianNewtonPush(-WorldTrajectory,UOP.GetWeight() + 1);
		}
		// NOTE: vanish doesn't require any extra logic. The item is already at hiddenpos and has
		// already been removed from the inventory system.

		foreach (var onMove in pickupable.GetComponents<IServerInventoryMove>())
		{
			onMove.OnInventoryMoveServer(toPerform);
		}

		if (pickupable.gameObject.TryGetComponent<Stackable>(out var stack))
		{
			var uop = pickupable.GetComponent<UniversalObjectPhysics>();
			stack.ServerStackOnGround(uop.transform.localPosition.RoundToInt());
		}

		return true;
	}

	private static bool ServerPerformAdd(InventoryMove toPerform, Pickupable pickupable)
	{
		// item is not currently in inventory, it should be moved into inventory system into
		// the indicated slot.

		if (pickupable.UniversalObjectPhysics.IsBuckled)
		{
			Loggy.LogTraceFormat("Attempted to add {0} to inventory but item is buckled to something {1}." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, pickupable.UniversalObjectPhysics.BuckledToObject);
			return false;
		}


		if (pickupable.ItemSlot != null)
		{
			Loggy.LogTraceFormat("Attempted to add {0} to inventory but item is already in slot {1}." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name, pickupable.ItemSlot);
			return false;
		}

		var toSlot = toPerform.ToSlot;
		if (toSlot == null)
		{
			Loggy.LogTraceFormat("Attempted to add {0} to inventory but target slot was null." +
			                      " Move will not be performed.", Category.Inventory, pickupable.name);
			return false;
		}

		if (toSlot.Item != null)
		{
			var stackableTarget = toSlot.Item.GetComponent<Stackable>();
			if (stackableTarget != null && stackableTarget.CanAccommodate(pickupable.gameObject))
			{
				toSlot.Item.GetComponent<Stackable>().ServerCombine(pickupable.GetComponent<Stackable>());
				return true;
			}
			else
			{
				switch (toPerform.ReplacementStrategy)
				{
					case ReplacementStrategy.DespawnOther:
						Loggy.LogTraceFormat("Attempted to add {0} to inventory but target slot {1} already had something in it." +
											  " Item in slot will be despawned first.", Category.Inventory, pickupable.name, toSlot);
						ServerDespawn(toSlot);
						break;
					case ReplacementStrategy.DropOther:
						Loggy.LogTraceFormat("Attempted to add {0} to inventory but target slot {1} already had something in it." +
											  " Item in slot will be dropped first.", Category.Inventory, pickupable.name, toSlot);
						ServerDrop(toSlot);
						break;
					case ReplacementStrategy.Cancel:
					default:
						Loggy.LogTraceFormat("Attempted to add {0} to inventory but target slot {1} already had something in it." +
											  " Move will not be performed.", Category.Inventory, pickupable.name, toSlot);
						return false;
				}
			}
		}

		if (!Validations.CanFit(toSlot, pickupable, NetworkSide.Server, true) && toPerform.IgnoreConstraints == false)
		{
			Loggy.LogTraceFormat("Attempted to add {0} to slot {1} but slot cannot fit this item." +
			                      " transfer will not be performed.", Category.Inventory, pickupable.name, toSlot);
			return false;
		}

		// no longer inside any PushPull
		pickupable.GetComponent<UniversalObjectPhysics>().DisappearFromWorld();

		// update pickupable's item and slot's item
		pickupable._SetItemSlot(toSlot);
		toSlot._ServerSetItem(pickupable);

		foreach (var onMove in pickupable.GetComponents<IServerInventoryMove>())
		{
			onMove.OnInventoryMoveServer(toPerform);
		}

		return true;
	}

	/// <summary>
	/// Client tells server to transfer items between 2 item slots, with client side prediction.
	/// One of the item slots must be either in this player's slot tree (i.e. currently owned by them
	/// even if nested within an item storage).
	///
	/// This method has validations to check this precondition before sending the message to the server,
	/// so feel free to just call this and not do any validation. It will fail with a Trace level
	/// message in Category.Inventory if it fails validation. It will also output an examine
	/// message to the player telling them why it failed.
	/// </summary>
	/// <param name="from">
	/// o</param>
	/// <param name="to"></param>
	/// <returns></returns>
	public static void ClientRequestTransfer(ItemSlot from, ItemSlot to)
	{
		if (!Validations.CanPutItemToSlot(PlayerManager.LocalPlayerScript, to, from.Item,
			NetworkSide.Client, PlayerManager.LocalPlayerObject, examineRecipient: PlayerManager.LocalPlayerObject))
		{
			Loggy.LogTraceFormat("Client cannot request transfer from {0} to {1} because" +
			                      " validation failed.", Category.Inventory,
				from, to);
			return;
		}

		//client side prediction, just change the sprite of the ui slots
		if (from.LocalUISlot != null)
		{
			from.LocalUISlot.Clear();
		}

		if (to.LocalUISlot != null)
		{
			to.LocalUISlot.UpdateImage(from.ItemObject);
		}

		//send the actual message.
		RequestInventoryTransferMessage.Send(from, to);
	}

	/// <summary>
	/// Use this to update the image displayed in a local UI slot when the object's sprite
	/// has changed somehow.
	/// If the provided object is in an item slot linked to a local UI slot, refreshes
	/// the local UI slot's image based on the object's current image.
	///
	/// If it's not in a slot, this has no effect.
	/// </summary>
	/// <param name="forObject"></param>
	public static void RefreshUISlotImage(GameObject forObject)
	{
		if (forObject == null) return;
		var pu = forObject.GetComponent<Pickupable>();
		if (pu != null)
		{
			pu.RefreshUISlotImage();
		}

	}

	/// <summary>
	/// Used to populate an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory,
	/// Recursively far down as specified in namedSlotPopulatorEntrys
	/// </summary>
	public static void PopulateSubInventory(GameObject gameObject, List<SlotPopulatorEntry> namedSlotPopulatorEntrys, SpawnInfo info)
	{
		if (namedSlotPopulatorEntrys.Count == 0) return;

		var itemStorage = gameObject.GetComponent<ItemStorage>();
		if (itemStorage == null) return;

		PopulateSubInventory(itemStorage, namedSlotPopulatorEntrys, info);
	}

	/// <summary>
	/// Used to populate an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory,
	/// Recursively far down as specified in namedSlotPopulatorEntrys
	/// </summary>
	public static void PopulateSubInventory(ItemStorage itemStorage, List<SlotPopulatorEntry> namedSlotPopulatorEntrys, SpawnInfo info)
	{
		if (namedSlotPopulatorEntrys.Count == 0) return;

		foreach (var namedSlotPopulatorEntry in namedSlotPopulatorEntrys)
		{
			if (namedSlotPopulatorEntry == null || namedSlotPopulatorEntry.Prefab == null) continue;
			ItemSlot ItemSlot;
			if (namedSlotPopulatorEntry.DoNotGetFirstEmptySlot == false)
			{
				ItemSlot =  itemStorage.GetNextEmptySlot();
			}
			else
			{
				if (namedSlotPopulatorEntry.UseIndex)
				{
					ItemSlot = itemStorage.GetIndexedItemSlot(namedSlotPopulatorEntry.IndexSlot);
				}
				else
				{
					ItemSlot = itemStorage.GetNamedItemSlot(namedSlotPopulatorEntry.NamedSlot);
				}

				if (ItemSlot.Item != null && namedSlotPopulatorEntry.IfOccupiedFindEmptySlot)
				{
					ItemSlot = itemStorage.GetNextFreeIndexedSlot();
				}
			}


			if (ItemSlot == null) continue;

			var spawn = Spawn.ServerPrefab(namedSlotPopulatorEntry.Prefab, PrePickRandom: true, spawnManualContents: info?.SpawnManualContents ?? false);

			if (Validations.CanFit(ItemSlot, spawn.GameObject, NetworkSide.Server) == false)
			{
				Loggy.LogError($"Your initial contents spawn for ItemStorage {itemStorage.name} for {spawn.GameObject} Is bypassing the Can fit requirements");
			}

			ServerAdd(spawn.GameObject, ItemSlot,namedSlotPopulatorEntry.ReplacementStrategy, true );
			PopulateSubInventoryRecursive(spawn.GameObject, namedSlotPopulatorEntry.namedSlotPopulatorEntrys, info);
		}
	}

	/// <summary>
	/// Used to populate an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory,
	/// Recursively far down as specified in SlotPopulatorEntryRecursive
	/// </summary>
	public static void PopulateSubInventoryRecursive(GameObject gameObject, List<SlotPopulatorEntryRecursive> namedSlotPopulatorEntrys, SpawnInfo info)
	{
		if (namedSlotPopulatorEntrys.Count == 0) return;

		var itemStorage = gameObject.GetComponent<ItemStorage>();
		if (itemStorage == null) return;

		PopulateSubInventoryRecursive(itemStorage, namedSlotPopulatorEntrys, info);
	}

	/// <summary>
	/// Used to populate an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory within an inventory,
	/// Recursively far down as specified in SlotPopulatorEntryRecursive
	/// </summary>
	public static void PopulateSubInventoryRecursive(ItemStorage itemStorage, List<SlotPopulatorEntryRecursive> namedSlotPopulatorEntrys, SpawnInfo info)
	{
		if (namedSlotPopulatorEntrys.Count == 0) return;

		foreach (var namedSlotPopulatorEntry in namedSlotPopulatorEntrys)
		{
			if (namedSlotPopulatorEntry == null || namedSlotPopulatorEntry.Prefab == null) continue;
			ItemSlot ItemSlot;
			if (namedSlotPopulatorEntry.DoNotGetFirstEmptySlot == false)
			{
				ItemSlot =  itemStorage.GetNextEmptySlot();
			}
			else
			{
				if (namedSlotPopulatorEntry.UseIndex)
				{
					ItemSlot = itemStorage.GetIndexedItemSlot(namedSlotPopulatorEntry.IndexSlot);
				}
				else
				{
					ItemSlot = itemStorage.GetNamedItemSlot(namedSlotPopulatorEntry.NamedSlot);
				}

				if (ItemSlot.Item != null && namedSlotPopulatorEntry.IfOccupiedFindEmptySlot)
				{
					ItemSlot = itemStorage.GetNextFreeIndexedSlot();
				}
			}

			var spawn = Spawn.ServerPrefab(namedSlotPopulatorEntry.Prefab, PrePickRandom: true, spawnManualContents: info?.SpawnManualContents ?? false);

			if (Validations.CanFit(ItemSlot, spawn.GameObject, NetworkSide.Server) == false)
			{
				Loggy.LogError($"Your initial contents spawn for ItemStorage {itemStorage.name} for {spawn.GameObject} Is bypassing the Can fit requirements");
			}

			ServerAdd(spawn.GameObject, ItemSlot,namedSlotPopulatorEntry.ReplacementStrategy, true);
		}
	}
}

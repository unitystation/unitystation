
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Represents a slot which can store an item (from a particular ItemStorage).
///
/// Unlike SlotIdentifier, it's an actual instance of a slot in an instance of ItemStorage.
///
/// There is only ever one instance of a particular ItemSlot in a given instance of the game! There is no
/// possibility of having 2 ItemSlot objects pointing at the same slot. This is accomplished using
/// an object pool / static factory.
///
/// On server side, this is authoritative. On client side, this represents clients current knowledge of each slot,
/// which may sometimes be out of date with server.
///
/// A client can be an "observer" of an item slot, in which case the server will keep them updated
/// whenever the slot changes.
/// </summary>
public class ItemSlot
{
	//object pool. Maps from the ItemStorage object's instance ID, to the Slot Identifier, to the actual item slot
	private static Dictionary<int, Dictionary<SlotIdentifier, ItemSlot>> slots =
		new Dictionary<int, Dictionary<SlotIdentifier, ItemSlot>>();

	// instance id of that itemStorage's object
	private readonly int itemStorageNetId;

	/// <summary>
	/// ItemStorage which contains this slot. Every slot has an item storage (except Invalid ones).
	/// </summary>
	public ItemStorage ItemStorage => itemStorage;
	private readonly ItemStorage itemStorage;

	/// <summary>
	/// Identifier of this slot within item storage
	/// </summary>
	public SlotIdentifier SlotIdentifier => slotIdentifier;
	private readonly SlotIdentifier slotIdentifier;

	/// <summary>
	/// Item in this slot, null if empty.
	/// </summary>
	public Pickupable Item => item;

	/// <summary>
	/// Net ID of the ItemStorage this slot exists in
	/// </summary>
	public uint ItemStorageNetID => itemStorage.GetComponent<NetworkIdentity>().netId;

	/// <summary>
	/// ItemAttributes of item in this slot, null if no item or item doesn't have any attributes.
	/// </summary>
	public ItemAttributesV2 ItemAttributes => item != null ? item.GetComponent<ItemAttributesV2>() : null;

	/// <summary>
	/// GameObject of item in this slot, null if no item in slot
	/// (Convenience method for not having to do item.gameObject and check for null)
	/// </summary>
	public GameObject ItemObject => item != null ? item.gameObject : null;

	/// <summary>
	/// RegisterPlayer this slot is in the top-level inventory of. Null if not on a player or
	/// in something like a backpack.
	/// </summary>
	public RegisterPlayer Player => itemStorage != null ? itemStorage.GetComponent<RegisterPlayer>() : null;

	/// <summary>
	/// RegisterPlayer this slot is in the slot tree of (i.e. even if in a backpack). Null if not on a player at all.
	/// </summary>
	public RegisterPlayer RootPlayer()
	{
		var root = GetRootStorage();
		if (root == null) return null;
		return root.GetComponent<RegisterPlayer>();
	}

	/// <summary>
	/// Named slot this item slot is identified by in this storage. Null if not a named slot.
	/// </summary>
	public NamedSlot? NamedSlot => slotIdentifier.NamedSlot;

	/// <summary>
	/// True iff the slot has no item.
	/// </summary>
	public bool IsEmpty => Item == null;
	/// <summary>
	/// True iff the slot has an item
	/// </summary>
	public bool IsOccupied => !IsEmpty;

	/// <summary>
	/// If this item slot is linked to the local player's UI slot, returns that UI slot. Otherwise
	/// returns null.
	/// </summary>
	public UI_ItemSlot LocalUISlot => localUISlot;

	private Pickupable item;
	private UI_ItemSlot localUISlot;

	/// <summary>
	/// True if this slot is no longer valid for any use, this only happens
	/// when the slot pool is cleaned up when a new round begins. It should not be possible to
	/// obtain a reference to an invalid slot since this happens on scene change, but this is here
	/// as a safeguard.
	/// </summary>
	public bool Invalid => invalid;
	private bool invalid;

	/// <summary>
	/// Server-side only. Players server thinks are currently looking at this slot (and thus will receive
	/// updates when the slot changes and can be allowed to perform transfers).
	/// </summary>
	private readonly HashSet<GameObject> serverObserverPlayers = new HashSet<GameObject>();

	/// <summary>
	/// Client side. Invoked after the contents of this slot are changed, which typically only happens
	/// when server sends us an update about this slot.
	/// Use this to update in response to such changes.
	/// </summary>
	public readonly UnityEvent OnSlotContentsChangeClient = new UnityEvent();
	/// <summary>
	/// Server side. Invoked after the contents of this slot are changed.
	/// Use this to update in response to such changes.
	/// </summary>
	public readonly UnityEvent OnSlotContentsChangeServer = new UnityEvent();

	private ItemSlot(ItemStorage itemStorage, SlotIdentifier slotIdentifier)
	{
		this.itemStorage = itemStorage;
		this.itemStorageNetId = itemStorage.GetInstanceID();
		this.slotIdentifier = slotIdentifier;
	}


	/// <summary>
	/// Gets the specified slot from the specified storage. Null if this item storage does not have
	/// a slot with the given identifier.
	/// </summary>
	/// <param name="itemStorage"></param>
	/// <param name="slotIdentifier"></param>
	/// <returns></returns>
	public static ItemSlot Get(ItemStorage itemStorage, SlotIdentifier slotIdentifier)
	{
		if (!itemStorage.HasSlot(slotIdentifier)) return null;

		var instanceID = itemStorage.GetInstanceID();
		slots.TryGetValue(instanceID, out var dict);
		if (dict == null)
		{
			dict = new Dictionary<SlotIdentifier, ItemSlot>();
			slots.Add(instanceID, dict);
		}

		dict.TryGetValue(slotIdentifier, out var slot);
		if (slot == null)
		{
			slot = new ItemSlot(itemStorage, slotIdentifier);
			dict.Add(slotIdentifier, slot);
		}

		return slot;
	}

	/// <summary>
	/// Gets the specified named slot from the specified storage. Null if this item storage does not have
	/// a slot with the given name.
	/// </summary>
	/// <param name="itemStorage"></param>
	/// <param name="named"></param>
	/// <returns></returns>
	public static ItemSlot GetNamed(ItemStorage itemStorage, NamedSlot named)
	{
		return Get(itemStorage, SlotIdentifier.Named(named));
	}

	/// <summary>
	/// Gets the specified indexed slot from the specified storage. Null if this item storage does not have
	/// a slot with the given name.
	/// </summary>
	/// <param name="itemStorage"></param>
	/// <param name="slotIndex"></param>
	/// <returns></returns>
	public static ItemSlot GetIndexed(ItemStorage itemStorage, int slotIndex)
	{
		return Get(itemStorage, SlotIdentifier.Indexed(slotIndex));
	}

	/// <summary>
	/// Server side only (can be called client side but has no effect). Add this player to the list of players currently observing this slot and
	/// informs this observer of the current state of the slot. This observer will be informed of any updates
	/// to this slot.
	/// </summary>
	/// <param name="observerPlayer"></param>
	public void ServerAddObserverPlayer(GameObject observerPlayer)
	{
		if (!CustomNetworkManager.IsServer) return;
		serverObserverPlayers.Add(observerPlayer);
		UpdateItemSlotMessage.Send(observerPlayer, this);
	}

	/// <summary>
	/// Server only (can be called client side but has no effect). Remove this player from the list of players currently observing this slot.
	/// This observer will not longer recieve updates as they happen to this slot.
	/// </summary>
	/// <param name="observerPlayer"></param>
	public void ServerRemoveObserverPlayer(GameObject observerPlayer)
	{
		if (!CustomNetworkManager.IsServer) return;
		serverObserverPlayers.Remove(observerPlayer);
		//tell the client the slot is now empty
		UpdateItemSlotMessage.Send(observerPlayer, this, true);
	}


	/// <summary>
	/// Server side only. Returns true if this player is observing this slot.
	/// </summary>
	/// <param name="observerPlayer"></param>
	/// <returns></returns>
	public bool ServerIsObservedBy(GameObject observerPlayer)
	{
		if (!CustomNetworkManager.IsServer) return false;
		return serverObserverPlayers.Contains(observerPlayer);
	}

	/// <summary>
	/// Gets the top-level ItemStorage containing this slot. I.e. if this
	/// is inside a crate in a backpack, will return the backpack ItemStorage.
	/// </summary>
	/// <returns></returns>
	public ItemStorage GetRootStorage()
	{
		return itemStorage.GetRootStorage();
	}

	public override string ToString()
	{
		return $"storage {itemStorage.name}, slot {slotIdentifier}, contains " + (Item != null ? Item.name : "nothing");
	}

	/// <summary>
	/// NOTE: Please use Inventory instead for moving inventory around.
	///
	/// Server-side only. Adds the specified item to the slot, updating any observers.
	/// Note that this doesn't do anything other than saying the item is now in the slot.
	/// </summary>
	public void _ServerSetItem(Pickupable newItem)
	{
		var removedItem = item;
		item = newItem;
		OnSlotContentsChangeServer.Invoke();

		//server has to call their own client side hooks because by the time the message is received,
		//the server will not be able to determine what slot the item came from.
		OnSlotContentsChangeClient.Invoke();
		if (removedItem != null)
		{
			//we displaced an item
			var info = ClientInventoryMove.OfType(ClientInventoryMoveType.Removed);
			foreach (var hook in removedItem.GetComponents<IClientInventoryMove>())
			{
				hook.OnInventoryMoveClient(info);
			}
		}

		if (newItem != null)
		{
			//we are adding an item to this slot
			var info = ClientInventoryMove.OfType(ClientInventoryMoveType.Added);
			foreach (var hook in newItem.GetComponents<IClientInventoryMove>())
			{
				hook.OnInventoryMoveClient(info);
			}
		}

		UpdateItemSlotMessage.Send(serverObserverPlayers, this);
	}

	/// <summary>
	/// NOTE: Please use Inventory instead for moving inventory around.
	///
	/// Server-side only. Remove the current item from the slot, updating any observers.
	/// Note that this doesn't do anything other than saying the item is no longer in the slot.
	/// </summary>
	public void _ServerRemoveItem()
	{
		_ServerSetItem(null);
	}

	/// <summary>
	/// Update these slot contents in response to message from server, firing appropriate
	/// hooks.
	/// </summary>
	/// <param name="newContents"></param>
	public void ClientUpdate(Pickupable newContents)
	{
		item = newContents;
		OnSlotContentsChangeClient.Invoke();
	}

	/// <summary>
	/// Checks if the indicated item can fit in this slot.
	/// </summary>
	/// <param name="toStore"></param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <param name="examineRecipient">if not null, when validation fails, will output an appropriate examine message to this recipient</param>
	/// <returns></returns>
	public bool CanFit(Pickupable toStore, bool ignoreOccupied = false, GameObject examineRecipient = null)
	{
		if (toStore == null) return false;

		ItemStorage storageToCheck = itemStorage;
		StorageIdentifier storeIdentifier = toStore.GetComponent<StorageIdentifier>();

		//Check if there is a deny entry for this toStore item
		if (storageToCheck != null && storeIdentifier != null)
		{
			InteractableStorage interactiveStorage = storageToCheck.GetComponent<InteractableStorage>();
			if (interactiveStorage != null)
			{
				if (interactiveStorage.denyStorageOfStorageItems.HasFlag(storeIdentifier.StorageItemName))
				{
					if (examineRecipient)
					{
						Chat.AddExamineMsg(examineRecipient, $"{toStore.gameObject.ExpensiveName()} can't be placed there!");
					}
					return false;
				}
			}
		}

		//go through this slot's ancestors and make sure none of them ARE toStore,
		//as that would create a loop in the inventory hierarchy
		int count = 0;
		while (storageToCheck != null)
		{
			if (storageToCheck.gameObject == toStore.gameObject)
			{
				Logger.LogTraceFormat(
					"Cannot fit {0} in slot {1}, this would create an inventory hierarchy loop (putting the" +
					" storage inside itself)", Category.Inventory, toStore, ToString());
				if (examineRecipient)
				{
					Chat.AddExamineMsg(examineRecipient, $"{toStore.gameObject.ExpensiveName()} can't go inside itself!");
				}
				return false;
			}
			//get parent item storage if it exists
			var pickupable = storageToCheck.GetComponent<Pickupable>();
			if (pickupable != null && pickupable.ItemSlot != null)
			{
				storageToCheck = pickupable.ItemSlot.ItemStorage;
			}
			else
			{
				storageToCheck = null;
			}

			count++;
			if (count > 5)
			{
				Logger.LogTraceFormat(
					"Something went wrong when adding {0} in slot {1}, aborting!", Category.Inventory, toStore, ToString());
				return false;
			}
		}

		//if the slot already has an item, it's allowed to stack only if the item to add can stack with
		//the existing item.
		if (!ignoreOccupied && item != null)
		{
			var thisStackable = item.GetComponent<Stackable>();
			var otherStackable = toStore.GetComponent<Stackable>();
			var stackResult = thisStackable != null && otherStackable != null &&
								thisStackable.CanAccommodate(otherStackable);
			if (!stackResult)
			{
				Logger.LogTraceFormat(
					"Cannot stack {0} in slot {1}", Category.Inventory, toStore, ToString());
			}
			else
			{
				Logger.LogTraceFormat(
					"Can stack {0} in slot {1}", Category.Inventory, toStore, ToString());
			}
			return stackResult;
		}

		//no item in slot and no inventory loop created,
		//check if this storage can fit this according to its specific capacity logic
		var canFit = itemStorage.ItemStorageCapacity.CanFit(toStore, this.slotIdentifier);
		if (canFit) return true;
		if (examineRecipient)
		{
			//if this is going in a player's inventory, use a more appropriate message.
			var targetPlayerScript = ItemStorage.GetComponent<PlayerScript>();
			if (targetPlayerScript != null)
			{
				//going into a top-level inventory slot of a player
				Chat.AddExamineMsg(examineRecipient, $"{toStore.gameObject.ExpensiveName()} can't go in that slot.");
			}
			else
			{
				//going into something else
				Chat.AddExamineMsg(examineRecipient, $"{toStore.gameObject.ExpensiveName()} doesn't fit in the {ItemStorage.gameObject.ExpensiveName()}.");
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if the indicated item can fit in this slot.
	/// </summary>
	/// <param name="pickupable">pickupable item</param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <returns></returns>
	public bool CanFit(GameObject pickupable, bool ignoreOccupied = false)
	{
		var pu = pickupable.GetComponent<Pickupable>();
		if (pu == null)
		{
			Logger.LogWarningFormat("{0} has no pickupable, thus can't fit anywhere. It's probably a bug that" +
								  " this was even attempted.", Category.Inventory, pickupable.name);
			return false;
		}

		return CanFit(pu, ignoreOccupied);

	}

	/// <summary>
	/// Can be called client and server side to free up the cached
	/// slots in this storage. Should be called when storage is going to be destroyed or
	/// will be no longer known by the client. On server side, also destroys all the items in the slot.
	/// </summary>
	/// <param name="storageToFree"></param>
	public static void Free(ItemStorage storageToFree)
	{
		if (CustomNetworkManager.Instance != null &&
		    CustomNetworkManager.Instance._isServer)
		{
			//destroy all items in the slots
			foreach (var slot in storageToFree.GetItemSlots())
			{
				if (slot.Item != null)
				{
					Inventory.ServerDespawn(slot);
				}
			}
		}

		var instanceID = storageToFree.GetComponent<NetworkIdentity>().GetInstanceID();
		slots.TryGetValue(instanceID, out var dict);
		if (dict != null)
		{
			dict.Clear();
			slots.Remove(instanceID);
		}
	}

	/// <summary>
	/// Removes entries from the pool for objects that no longer exist.
	/// Some components cache slots in their Awake methods so we can't simply delete the entire pool
	/// or those objects will be left with a reference to an invalid slot
	/// </summary>
	public static void Cleanup()
	{
		var instanceIdsToRemove = new List<int>();
		//find existing storage instance IDs which have no slots or which are for no longer existing objects
		foreach (var instanceIdToSlots in slots)
		{
			var instanceID = instanceIdToSlots.Key;
			if (instanceIdToSlots.Value == null || instanceIdToSlots.Value.Keys.Count == 0)
			{
				instanceIdsToRemove.Add(instanceID);
				continue;
			}

			//The dictionary doesn't have a mapping to ItemStorage objects, and
			//we can't use the instance ID to look up the game object except in editor,
			//so we instead figure out which game object this instance ID maps to by looking at
			//one of its slots and getting its item storage field
			var slotWithStorage = instanceIdToSlots.Value.Values
				.Where(slot => slot.itemStorage != null)
				.Select(slot => slot.itemStorage).FirstOrDefault();

			//we only keep the slot if it is for an object that actually exists,
			//i.e. the ItemStorage is not null and the ItemStorage gameObject is not null
			//since unity null check on game objects will tell us if object still exists.
			if (slotWithStorage == null || slotWithStorage.gameObject == null)
			{
				instanceIdsToRemove.Add(instanceID);
				continue;
			}
		}

		//now perform the actual removals and mark removed slots as invalid.
		foreach (var instanceId in instanceIdsToRemove)
		{
			var slotDict = slots[instanceId];
			//mark the slots as invalid
			foreach (var slot in slotDict.Values)
			{
				slot.invalid = true;
			}
			//delete
			slotDict.Clear();
			slots.Remove(instanceId);
		}
	}

	/// <summary>
	/// Links this item slot to the given client side UI slot.
	/// </summary>
	/// <param name="toLink"></param>
	public void LinkLocalUISlot(UI_ItemSlot toLink)
	{
		this.localUISlot = toLink;
	}

	/// <summary>
	/// Convenience method for getting an item slot when it's not known if it is indexed or
	/// named. If slotIndex == -1, will try to get it as a named slot, otherwise will get it
	/// as an indexed slot.
	/// </summary>
	/// <param name="itemStorage"></param>
	/// <param name="namedSlot"></param>
	/// <param name="slotIndex"></param>
	/// <returns></returns>
	public static ItemSlot Get(ItemStorage itemStorage, NamedSlot namedSlot, int slotIndex)
	{
		if (slotIndex == -1)
		{
			return ItemSlot.GetNamed(itemStorage, namedSlot);
		}
		return ItemSlot.GetIndexed(itemStorage, slotIndex);
	}

}


using System.Collections.Generic;
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
	/// ItemStorage which contains this slot
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
	public ItemAttributes ItemAttributes => item != null ? item.GetComponent<ItemAttributes>() : null;

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
	public bool IsEmpty => Item != null;

	/// <summary>
	/// If this item slot is linked to the local player's UI slot, returns that UI slot. Otherwise
	/// returns null.
	/// </summary>
	public UI_ItemSlot LocalUISlot => localUISlot;

	private Pickupable item;
	private UI_ItemSlot localUISlot;

	/// <summary>
	/// Server-side only. Players server thinks are currently looking at this slot (and thus will receive
	/// updates when the slot changes and can be allowed to perform transfers).
	/// </summary>
	private readonly HashSet<GameObject> serverObserverPlayers = new HashSet<GameObject>();

	/// <summary>
	/// Client side. Invoked after the contents of this slot are changed.
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
		ItemStorage storage = itemStorage;
		var pickupable = storage.GetComponent<Pickupable>();
		while (pickupable != null && pickupable.ItemSlot != null)
		{
			storage = pickupable.ItemSlot.itemStorage;
			pickupable = storage.GetComponent<Pickupable>();
		}

		return itemStorage;
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
	public void ServerSetItem(Pickupable newItem)
	{
		item = newItem;
		OnSlotContentsChangeServer.Invoke();
		UpdateItemSlotMessage.Send(serverObserverPlayers, this);
	}

	/// <summary>
	/// NOTE: Please use Inventory instead for moving inventory around.
	///
	/// Server-side only. Remove the current item from the slot, updating any observers.
	/// Note that this doesn't do anything other than saying the item is no longer in the slot.
	/// </summary>
	public void ServerRemoveItem()
	{
		ServerSetItem(null);
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
	/// <param name="pickupable"></param>
	/// <param name="ignoreOccupied">if true, does not check if an item is already in the slot</param>
	/// <returns></returns>
	public bool CanFit(Pickupable pickupable, bool ignoreOccupied = false)
	{
		if (!ignoreOccupied && item != null) return false;

		return itemStorage.ItemStorageCapacity.CanFit(pickupable, this.slotIdentifier);
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
		if (CustomNetworkManager.IsServer)
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
	/// Completely clears out the slot pool.
	/// </summary>
	public static void EmptyPool()
	{
		slots = new Dictionary<int, Dictionary<SlotIdentifier, ItemSlot>>();
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
	/// <exception cref="NotImplementedException"></exception>
	public static ItemSlot Get(ItemStorage itemStorage, NamedSlot namedSlot, int slotIndex)
	{
		if (slotIndex == -1)
		{
			return ItemSlot.GetNamed(itemStorage, namedSlot);
		}
		return ItemSlot.GetIndexed(itemStorage, slotIndex);
	}

}
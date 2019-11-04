
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Allows an object to store items.
/// ANYTHING which can contain items would have this component (that includes players,
/// player inventory is Storage).
///
/// The ways in which the storage can be interacted with is handled by other components.
///
/// Note that items stored in an ItemStorage can themselves have ItemStorage (for example, storing a backpack
/// in a player's inventory)!
/// </summary>
public class ItemStorage : MonoBehaviour, IServerSpawn, IServerDespawn
{
	[Tooltip("Configuration describing the structure of the slots - i.e. what" +
	         " the slots are / how many there are.")]
	public ItemStorageStructure ItemStorageStructure;

	[Tooltip("Capacity of this storage - what each slot is allowed to hold.")]
	public ItemStorageCapacity ItemStorageCapacity;

	[Tooltip("Defines how the storage should be populated when the object spawns. You can also" +
	         " invoke Populate to manually / dynamically populate this storage using a supplied populator." +
	         " This will only run server side.")]
	public ItemStoragePopulator ItemStoragePopulator;

	/// <summary>
	/// Cached for quick lookup of what slots are actually available in this storage.
	/// </summary>
	private HashSet<SlotIdentifier> definedSlots;

	//note this will be null if this is not a player's own top-level inventory
	private PlayerNetworkActions playerNetworkActions;

	private void Awake()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		CacheDefinedSlots();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ServerPopulate(ItemStoragePopulator, PopulationContext.AfterSpawn(info));
	}

	private void CacheDefinedSlots()
	{
		if (ItemStorageStructure == null)
		{
			Logger.LogErrorFormat("{0} has ItemStorage but no defined ItemStorageStructure. Item storage will not work." +
			                      " Please define an ItemStorageStructure for this prefab.", Category.Inventory, name);
			return;
		}

		foreach (var slot in ItemStorageStructure.Slots())
		{
			definedSlots.Add(slot);
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="slotIdentifier"></param>
	/// <returns>true iff this item storage has the slot with the specified identifier</returns>
	public bool HasSlot(SlotIdentifier slotIdentifier)
	{
		if (definedSlots == null)
		{
			CacheDefinedSlots();
		}

		return definedSlots.Contains(slotIdentifier);
	}

	/// <summary>
	/// Server-side only. Populate this ItemStorage using the specified populator. Destroys any existing
	/// items in this storage.
	/// </summary>
	/// <param name="populator"></param>
	/// <param name="context">context of the population</param>
	public void ServerPopulate(IItemStoragePopulator populator, PopulationContext context)
	{
		if (!CustomNetworkManager.IsServer) return;
		populator.PopulateItemStorage(this, context);
	}

	/// <summary>
	/// Gets all item slots in this storage. Note that some item slots may themselves contain
	/// ItemStorage, and so on (a tree-like structure). This method does NOT recursively retrieve slots within
	/// items in slots, it only retrieves the slots defined in this particular ItemStorage. If you want to get
	/// the full tree of slots, use ItemSlotTree
	/// </summary>
	public IEnumerable<ItemSlot> GetItemSlots()
	{
		return definedSlots.Select(GetItemSlot);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="named"></param>
	/// <returns>the item slot from this storage, null if this item storage doesn't have this slot</returns>
	public ItemSlot GetItemSlot(SlotIdentifier named)
	{
		return ItemSlot.Get(this, named);
	}
	public ItemSlot GetNamedItemSlot(NamedSlot named)
	{
		return ItemSlot.GetNamed(this, named);
	}
	public ItemSlot GetIndexedItemSlot(int slotIndex)
	{
		return ItemSlot.GetIndexed(this, slotIndex);
	}

	/// <summary>
	/// Server-side only. Destroys all items in inventory.
	/// </summary>
	public void ServerClear()
	{
		foreach (var slot in GetItemSlots())
		{
			if (slot.Item != null)
			{
				Despawn.ServerSingle(slot.Item.gameObject);
			}
		}
	}

	/// <summary>
	/// Gets all item slots in this and all contained item storages. Basically
	/// gets every single item slot that exists somewhere in the hierarchy of storage
	/// contained in this storage.
	///
	/// As you should expect, this can create a bit of garbage so use sparingly.
	/// </summary>
	public IEnumerable<ItemSlot> GetItemSlotTree()
	{
		//not sure if this blows the heap up since it's recursive selectmany, but it's trivial to convert
		//to BFS / DFS if needed.
		return GetItemSlots().SelectMany(SlotSubtree);
	}

	private IEnumerable<ItemSlot> SlotSubtree(ItemSlot slot)
	{
		if (slot.Item == null)
		{
			return new[] {slot};
		}

		var itemStorage = slot.Item.GetComponent<ItemStorage>();
		if (itemStorage != null)
		{
			return itemStorage.GetItemSlots();
		}
		else
		{
			return new[] {slot};
		}
	}

	public IEnumerable<ItemSlot> GetIndexedSlots()
	{
		return GetItemSlots()
			.Where(its => its.SlotIdentifier.SlotIdentifierType == SlotIdentifierType.Indexed);
	}

	/// <summary>
	/// Gets the next free indexed slot. Null if none.
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetNextFreeIndexedSlot()
	{
		return GetIndexedSlots().FirstOrDefault(its => its.Item == null);
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//reclaim the space in the slot pool.
		ItemSlot.Free(this);
	}

	/// <summary>
	/// Gets all slots in which a gas container can be stored and used
	/// </summary>
	/// <returns></returns>
	public IEnumerable<ItemSlot> GetGasSlots()
	{
		return GetItemSlots().Where(its =>
			GasUseSlots.Contains(its.SlotIdentifier.NamedSlot.GetValueOrDefault(NamedSlot.none)));
	}

	/// <summary>
	/// Slots gas containers can be used from.
	/// </summary>
	public static readonly NamedSlot[] GasUseSlots = 	{NamedSlot.leftHand, NamedSlot.rightHand, NamedSlot.storage01, NamedSlot.storage02,
		NamedSlot.suitStorage, NamedSlot.back, NamedSlot.belt};

	/// <summary>
	/// Gets the highest indexed slot that is currently occupied. Null if none are occupied
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetTopOccupiedIndexedSlot()
	{
		return GetIndexedSlots().LastOrDefault(ids => ids.Item != null);
	}

	/// <summary>
	/// The item slot representing the active hand. Null if this is not a player.
	/// </summary>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public ItemSlot GetActiveHandSlot()
	{
		if (playerNetworkActions == null) return null;
		return GetNamedItemSlot(playerNetworkActions.activeHand);
	}
}

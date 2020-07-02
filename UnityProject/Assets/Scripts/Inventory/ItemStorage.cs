
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Allows an object to store items.
/// ANYTHING which can contain items would have this component (that includes players,
/// player inventory is Storage).
///
/// The ways in which the storage can be interacted with is handled by other components.
///
/// Note that items stored in an ItemStorage can themselveOnDespawnServer
/// </summary>
public class ItemStorage : MonoBehaviour, IServerLifecycle, IServerInventoryMove, IClientInventoryMove
{
	[SerializeField]
	[FormerlySerializedAs("ItemStorageStructure")]
	[Tooltip("Configuration describing the structure of the slots - i.e. what" +
			 " the slots are / how many there are.")]
	private ItemStorageStructure itemStorageStructure = null;

	/// <summary>
	/// Storage structure of this object
	/// </summary>
	public ItemStorageStructure ItemStorageStructure => itemStorageStructure;

	[SerializeField]
	[FormerlySerializedAs("ItemStorageCapacity")]
	[Tooltip("Capacity of this storage - what each slot is allowed to hold.")]
	private ItemStorageCapacity itemStorageCapacity = null;

	/// <summary>
	/// Storage capacity of this object
	/// </summary>
	public ItemStorageCapacity ItemStorageCapacity => itemStorageCapacity;

	[FormerlySerializedAs("ItemStoragePopulator")]
	[SerializeField]
	[Tooltip("Defines how the storage should be populated when the object spawns. You can also" +
	         " invoke Populate to manually / dynamically populate this storage using a supplied populator." +
	         " This will only run server side.")]
	private ItemStoragePopulator itemStoragePopulator = null;

	/// <summary>
	/// Cached for quick lookup of what slots are actually available in this storage.
	/// </summary>
	private HashSet<SlotIdentifier> definedSlots;

	//note this will be null if this is not a player's own top-level inventory
	private PlayerNetworkActions playerNetworkActions;

	/// <summary>
	/// Server-side only. Players server thinks are currently looking at this storage.
	/// </summary>
	private readonly HashSet<GameObject> serverObserverPlayers = new HashSet<GameObject>();

	private void Awake()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		CacheDefinedSlots();
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		ServerPopulate(itemStoragePopulator, PopulationContext.AfterSpawn(info));
		//if this is a player's inventory, make them an observer of all slots
		if (GetComponent<PlayerScript>() != null)
		{
			ServerAddObserverPlayer(gameObject);
		}
	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//if this is a player's inventory, make them no longer an observer of all slots
		if (GetComponent<PlayerScript>() != null)
		{
			ServerRemoveObserverPlayer(gameObject);
		}
		//reclaim the space in the slot pool.
		ItemSlot.Free(this);
	}

	private void OnDestroy()
	{
		//free the slots
		ItemSlot.Free(this);
	}


	public void OnInventoryMoveServer(InventoryMove info)
	{
		var fromRootPlayer = info.FromRootPlayer;
		var toRootPlayer = info.ToRootPlayer;
		//no need to do anything, hasn't moved into player inventory
		if (fromRootPlayer == toRootPlayer) return;

		//when this storage changes ownership to another player, the new owner becomes an observer of each slot in the slot
		//tree.
		//When it leaves ownership of another player, the previous owner no longer observes each slot in the slot tree.
		if (fromRootPlayer != null)
		{
			ServerRemoveObserverPlayer(info.FromRootPlayer.gameObject);
		}

		if (toRootPlayer != null)
		{
			ServerAddObserverPlayer(info.ToRootPlayer.gameObject);
		}
	}


	/// <summary>
	/// Gets the top-level ItemStorage containing this storage. I.e. if this
	/// is a crate inside a backpack will return the backpack ItemStorage. If this is not in anything
	/// will simply return this
	/// </summary>
	/// <returns></returns>
	public ItemStorage GetRootStorage()
	{
		ItemStorage storage = this;
		var pickupable = storage.GetComponent<Pickupable>();
		while (pickupable != null && pickupable.ItemSlot != null)
		{
			storage = pickupable.ItemSlot.ItemStorage;
			pickupable = storage.GetComponent<Pickupable>();
		}

		return storage;
	}

	public void OnInventoryMoveClient(ClientInventoryMove info)
	{
		if (UIManager.StorageHandler == null) return;

		//if we were currently looking at this storage, close the storage UI if this item was moved at all.
		if (UIManager.StorageHandler.CurrentOpenStorage == this)
		{
			UIManager.StorageHandler.CloseStorageUI();
		}
	}

	private void CacheDefinedSlots()
	{
		if (itemStorageStructure == null)
		{
			Logger.LogErrorFormat("{0} has ItemStorage but no defined ItemStorageStructure. Item storage will not work." +
			                      " Please define an ItemStorageStructure for this prefab.", Category.Inventory, name);
			return;
		}

		definedSlots = new HashSet<SlotIdentifier>();
		foreach (var slot in itemStorageStructure.Slots())
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
	///
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns>true iff this item storage has the slot with the specified identifier</returns>
	public bool HasSlot(NamedSlot namedSlot)
	{
		return HasSlot(SlotIdentifier.Named(namedSlot));
	}

	/// <summary>
	/// Server-side only. Populate this ItemStorage using the specified populator. Destroys any existing
	/// items in this storage.
	/// </summary>
	/// <param name="populator"></param>
	/// <param name="context">context of the population</param>
	public void ServerPopulate(IItemStoragePopulator populator, PopulationContext context)
	{
		if (populator == null) return;
		if (!CustomNetworkManager.IsServer) return;
		if (!context.SpawnInfo.SpawnItems) return;
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
			return itemStorage.GetItemSlots().Concat(new[] {slot});
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

	/// <summary>
	/// Returns the best slot (according to BestSlotForTrait) that is capable of holding
	/// this item (or any arbitrary slot if none of the best slots are capable of holding it).
	/// Returns null if there is no slot in this storage that can fit the item.
	/// Works for indexed and named slots both.
	/// </summary>
	/// <param name="toCheck"></param>
	/// <returns></returns>
	public ItemSlot GetBestSlotFor(Pickupable toCheck)
	{
		return BestSlotForTrait.Instance.GetBestSlot(toCheck, this, false);
	}

	/// <summary>
	/// Returns the best slot (according to BestSlotForTrait) that is capable of holding
	/// this item (or any arbitrary slot if none of the best slots are capable of holding it).
	/// Returns null if there is no slot in this storage that can fit the item.
	/// Works for indexed and named slots both.
	/// </summary>
	/// <param name="toCheck"></param>
	/// <returns></returns>
	public ItemSlot GetBestSlotFor(GameObject toCheck)
	{
		if (toCheck == null) return null;
		return GetBestSlotFor(toCheck.GetComponent<Pickupable>());
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
	public ItemSlot GetActiveHandSlot()
	{
		if (playerNetworkActions == null) return null;
		return GetNamedItemSlot(playerNetworkActions.activeHand);
	}

	/// <summary>
	/// Server only (can be called client side but has no effect).
	/// Add this player to the list of players currently observing all slots in the slot tree
	/// This observer will receive updates as they happen to this slot and will
	/// receieve an update for each slot in the tree immediately as the result
	/// of this method.
	/// </summary>
	/// <param name="observerPlayer"></param>
	public void ServerAddObserverPlayer(GameObject observerPlayer)
	{
		if (!CustomNetworkManager.IsServer) return;
		serverObserverPlayers.Add(observerPlayer);
		foreach (var slot in GetItemSlotTree())
		{
			slot.ServerAddObserverPlayer(observerPlayer);
		}
	}

	/// <summary>
	/// Server only (can be called client side but has no effect).
	/// Remove this player from the list of players currently observing all slots in the slot tree
	/// This observer will not longer receive updates as they happen to this slot.
	/// </summary>
	/// <param name="observerPlayer"></param>
	public void ServerRemoveObserverPlayer(GameObject observerPlayer)
	{
		if (!CustomNetworkManager.IsServer) return;
		serverObserverPlayers.Remove(observerPlayer);
		foreach (var slot in GetItemSlotTree())
		{
			slot.ServerRemoveObserverPlayer(observerPlayer);
		}
	}

	/// <summary>
	/// Server only (can be called client side but has no effect).
	/// Removes every observer player from the list of players currently observing all slots in the slot tree,
	/// except for the player who is holding this.
	/// </summary>
	/// <param name="observerPlayer"></param>
	public void ServerRemoveAllObserversExceptOwner()
	{
		if (!CustomNetworkManager.IsServer) return;
		var rootStorage = GetRootStorage();
		//have to do it this way so we don't get a concurrent modification error
		var observersToRemove = serverObserverPlayers.Where(obs => obs != rootStorage.gameObject).ToArray();
		foreach (var observerPlayer in observersToRemove)
		{
			ServerRemoveObserverPlayer(observerPlayer);
		}
	}


	/// <summary>
	/// Checks if the indicated player is an observer of this storage
	/// </summary>
	/// <param name="observer"></param>
	/// <returns></returns>
	public bool ServerIsObserver(GameObject observer)
	{
		return serverObserverPlayers.Contains(observer);
	}

	/// <summary>
	/// Drops all items in all slots at our current position.
	/// </summary>
	public void ServerDropAll()
	{
		foreach (var itemSlot in GetItemSlots())
		{
			Inventory.ServerDrop(itemSlot);
		}
	}
}

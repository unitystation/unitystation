using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using NaughtyAttributes;
using Systems.Storage;
using Items;
using Logs;

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

	public ItemStoragePopulator ItemStoragePopulator => itemStoragePopulator;

	[Tooltip("Force spawn contents at round start rather than first open")]
	public bool forceSpawnContents;

	public bool ManuallySpawnContent = false;

	private bool contentsSpawned;
	public bool ContentsSpawned => contentsSpawned;

	/// <summary>
	/// Cached for quick lookup of what slots are actually available in this storage.
	/// </summary>
	private HashSet<SlotIdentifier> definedSlots;
	public HashSet<SlotIdentifier> DefinedSlots => new(definedSlots);

	//note this will be null if this is not a player's own top-level inventory
	private PlayerNetworkActions playerNetworkActions;

	/// <summary>
	/// Server-side only. Players server thinks are currently looking at this storage.
	/// </summary>
	private readonly HashSet<GameObject> serverObserverPlayers = new HashSet<GameObject>();

	//This is called when an itemslot in the item storage has its item set.
	//It can be null, or it can be a pickupable.(Health V2)
	public event Action<Pickupable, Pickupable> ServerInventoryItemSlotSet;

	[SerializeField] private bool dropItemsOnDespawn;

	public bool UesAddlistPopulater = false;

	[ShowIf(nameof(UesAddlistPopulater))] public PrefabListPopulater Populater;

	private SpawnInfo spawnInfo;

	public RegisterPlayer Player => player;
	private RegisterPlayer player;

	public bool SetSlotItemNotRemovableOnStartUp = false;

	public void SetRegisterPlayer(RegisterPlayer registerPlayer)
	{
		player = registerPlayer;
	}


	[SerializeField] private GameObject ashPrefab;
	public GameObject AshPrefab => ashPrefab;

	private void Awake()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		CacheDefinedSlots();
		if (SetSlotItemNotRemovableOnStartUp)
		{
			foreach (var slot in GetItemSlots())
			{
				slot.ItemNotRemovable = true;
			}
		}
	}

	public void OnSpawnServer(SpawnInfo info)
	{
		spawnInfo = info;

		if (forceSpawnContents)
		{
			TrySpawnContents(info);
		}

		//if this is a player's inventory, make them an observer of all slots
		if (GetComponent<PlayerScript>() != null)
		{
			ServerAddObserverPlayer(gameObject);
		}
	}

	public void TrySpawnContents(SpawnInfo info = null)
	{
		if (contentsSpawned || spawnInfo == null) return;
		contentsSpawned = true;

		if (ManuallySpawnContent == false || info?.SpawnManualContents == true)
		{
			ServerPopulate(itemStoragePopulator, PopulationContext.AfterSpawn(spawnInfo), info);
			if (UesAddlistPopulater)
			{
				ServerPopulate(Populater, PopulationContext.AfterSpawn(spawnInfo), info);
			}
		}

	}

	public void OnDespawnServer(DespawnInfo info)
	{
		//if this is a player's inventory, make them no longer an observer of all slots
		if (GetComponent<PlayerScript>() != null)
		{
			ServerRemoveObserverPlayer(gameObject);
		}

		if (dropItemsOnDespawn)
			ServerDropAll();

		//reclaim the space in the slot pool.
		ItemSlot.Free(this);
	}


	public bool ServerTrySpawnAndAdd(GameObject inGameObject)
	{
		var spawned = Spawn.ServerPrefab(inGameObject);
		if (spawned.Successful == false) return false;
		return ServerTryAdd(spawned.GameObject);
	}

	//True equals successful false equals unsuccessful
	public bool ServerTryAdd(GameObject inGameObject)
	{
		var item = inGameObject.GetComponent<ItemAttributesV2>();
		if (item == null) return false;
		var slot = GetBestSlotFor(inGameObject);
		if (slot == null) return false;

		return Inventory.ServerAdd(inGameObject, slot);
	}

	public bool ServerTransferGameObjectToItemSlot(GameObject outGameObject, ItemSlot Slot)
	{
		var item = outGameObject.GetComponent<ItemAttributesV2>();
		if (item == null) return false;
		var slot = GetSlotFromItem(outGameObject);
		if (slot == null) return false;
		return Inventory.ServerTransfer(slot, Slot);
	}

	public ItemSlot GetSlotFromItem(GameObject gameObject)
	{
		foreach (var itemSlot in GetItemSlots())
		{
			if (itemSlot.Item == null) continue;
			if (itemSlot.Item.gameObject == gameObject)
			{
				return itemSlot;
			}
		}

		return null;
	}

	public bool ServerTryTransferFrom(ItemSlot inSlot)
	{
		var Item = inSlot.Item.GetComponent<ItemAttributesV2>();
		if (Item == null) return false;
		var slot = GetBestSlotFor(inSlot.Item);
		if (slot == null) return false;

		return Inventory.ServerTransfer(inSlot, slot, ReplacementStrategy.Cancel);
	}

	public bool ServerTryTransferFrom(GameObject inSlot)
	{
		ItemAttributesV2 Item = inSlot.Item().GetComponent<ItemAttributesV2>();
		if (Item == null) return false;
		ItemSlot slot = GetBestSlotFor(inSlot.gameObject.GetComponent<Pickupable>());
		if (slot == null) return false;

		return Inventory.ServerTransfer(inSlot.gameObject.GetComponent<Pickupable>().ItemSlot, slot,
			ReplacementStrategy.Cancel);
	}

	public bool ServerTryRemove(GameObject InGameObject, bool Destroy = false,
		Vector3? DroppedAtWorldPositionOrThrowVector = null,
		bool Throw = false)
	{
		var slots = GetItemSlots();
		foreach (var slot in slots)
		{
			if (slot.Item.OrNull()?.gameObject != InGameObject) continue;
			if (Destroy)
			{
				return Inventory.ServerDespawn(slot);
			}

			if (Throw)
			{
				return DroppedAtWorldPositionOrThrowVector != null
					? Inventory.ServerThrow(slot, DroppedAtWorldPositionOrThrowVector.GetValueOrDefault())
					: Inventory.ServerThrow(slot, Vector2.zero);
			}
			else
			{
				return DroppedAtWorldPositionOrThrowVector != null
					? Inventory.ServerDrop(slot, DroppedAtWorldPositionOrThrowVector.GetValueOrDefault())
					: Inventory.ServerDrop(slot);
			}
		}

		return false;
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		var fromRootPlayer = info.FromRootPlayer;
		var toRootPlayer = info.ToRootPlayer;

		var Slots = GetItemSlots();
		foreach (var Slot in Slots)
		{
			if (Slot.Item != null)
			{
				var Moves = Slot.Item.GetComponents<IServerInventoryMove>();
				foreach (var Move in Moves)
				{
					try
					{
						Move.OnInventoryMoveServer(info);
					}
					catch (Exception e)
					{
						Loggy.LogError(e.ToString());
					}
				}
			}
		}

		//no need to do anything, hasn't moved into player inventory
		if (fromRootPlayer == toRootPlayer) return;

		//when this storage changes ownership to another player, the new owner becomes an observer of each slot in the slot
		//tree.
		//When it leaves ownership of another player, the previous owner no longer observes each slot in the slot tree.
		if (fromRootPlayer != null)
		{
			SetRegisterPlayer(null);
			ServerRemoveObserverPlayer(info.FromRootPlayer.gameObject);
		}

		if (toRootPlayer != null)
		{
			SetRegisterPlayer(info.ToRootPlayer);
			ServerAddObserverPlayer(info.ToRootPlayer.gameObject);
		}
	}

	/// <summary>
	/// Return the size of the storage.
	/// </summary>
	public int StorageSize()
	{
		return definedSlots.Count;
	}

	public void OnInventorySlotSet(Pickupable prevItem, Pickupable newItem)
	{
		//If we have any actions stored, invoke em. (Health V2)
		ServerInventoryItemSlotSet?.Invoke(prevItem, newItem);
	}

	/// <summary>
	/// Gets the top-level ItemStorage containing this storage. I.e. if this
	/// is a crate inside a backpack will return the crate ItemStorage. If this is not in anything
	/// will simply return this
	/// </summary>
	/// <returns></returns>
	public GameObject GetRootStorageOrPlayer()
	{
		try
		{
			ItemStorage storage = GetRootStorage();
			if (storage.player != null)
			{
				return storage.player.gameObject;
			}

			return storage.gameObject;
		}
		catch (NullReferenceException exception)
		{
			Loggy.LogError($"Caught NRE in ItemStorage: {exception.Message} \n {exception.StackTrace}",
				Category.Inventory);
			return null;
		}
	}

	/// <summary>
	/// Gets the top-level ItemStorage containing this storage. I.e. if this
	/// is a crate inside a backpack will return the crate ItemStorage. If this is not in anything
	/// will simply return this
	/// </summary>
	/// <returns></returns>
	public ItemStorage GetRootStorage()
	{
		try
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
		catch (NullReferenceException exception)
		{
			Loggy.LogError($"Caught NRE in ItemStorage: {exception.Message} \n {exception.StackTrace}",
				Category.Inventory);
			return null;
		}
	}

	/// <summary>
	/// Change the number of available slots in the storage.
	/// </summary>
	public void AcceptNewStructure(ItemStorageStructure newStructure)
	{
		itemStorageStructure = newStructure;
		definedSlots = null;
		CacheDefinedSlots();
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
			Loggy.LogErrorFormat(
				"{0} has ItemStorage but no defined ItemStorageStructure. Item storage will not work." +
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
	public void ServerPopulate(IItemStoragePopulator populator, PopulationContext context, SpawnInfo info)
	{
		if (populator == null) return;
		if (!CustomNetworkManager.IsServer) return;
		if (!context.SpawnInfo.SpawnItems) return;
		populator.PopulateItemStorage(this, context, info);
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

	public ItemSlot GetNextEmptySlot()
	{
		var slots = GetItemSlots();
		foreach (var slot in slots)
		{
			if (slot.Item == null)
			{
				return slot;
			}
		}

		return null;
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
				_ = Despawn.ServerSingle(slot.Item.gameObject);
			}
		}
	}

	/// <summary>
	/// Gets all item slots in this and all contained item storages. Basically
	/// gets every single item slot that exists somewhere in the hierarchy of storage
	/// contained in this storage.
	///
	/// As you should expect, this can create a bit of garbage so use sparingly.
	///
	/// </summary>
	public IEnumerable<ItemSlot> GetItemSlotTree()
	{
		//not sure if this blows the heap up since it's recursive selectmany, but it's trivial to convert
		//to BFS / DFS if needed.
		return GetItemSlots().SelectMany(SlotSubtree);
	}

	public static IEnumerable<ItemSlot> SlotSubtree(ItemSlot slot)
	{
		if (slot.Item == null)
		{
			return new[] { slot };
		}

		var itemStorage = slot.Item.GetComponents<ItemStorage>();
		var ListThis = new List<ItemSlot>();
		foreach (var itemStorages in itemStorage)
		{
			ListThis.AddRange(itemStorages.GetItemSlotTree());
		}

		var ToReturn = ListThis.ToArray().Concat(new[] { slot });
		return ToReturn;
	}

	public IEnumerable<ItemSlot> GetIndexedSlots()
	{
		return GetItemSlots()
			.Where(its => its.SlotIdentifier.SlotIdentifierType == SlotIdentifierType.Indexed);
	}

	public IEnumerable<ItemSlot> GetNamedItemSlots()
	{
		return GetItemSlots()
			.Where(its => its.SlotIdentifier.SlotIdentifierType == SlotIdentifierType.Named);
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
	/// Gets the next free indexed slot. Null if none.
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetNextFreeNamedSlot()
	{
		return GetNamedItemSlots().FirstOrDefault(its => its.Item == null);
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
	/// Gets the highest indexed slot that is currently occupied. Null if none are occupied
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetTopOccupiedIndexedSlot()
	{
		return GetIndexedSlots().LastOrDefault(ids => ids.Item != null);
	}

	/// <summary>
	/// Gets the highest indexed slot that is currently occupied. Null if none are occupied
	/// </summary>
	/// <returns></returns>
	public List<ItemSlot> GetOccupiedSlots()
	{
		var result = new List<ItemSlot>();
		foreach (var slot in GetIndexedSlots())
		{
			if (slot.IsOccupied) result.Add(slot);
		}

		return result;
	}


	/// <summary>
	/// Returns if any slot is occupied
	/// </summary>
	/// <returns></returns>
	public bool HasAnyOccupied()
	{
		return GetIndexedSlots().Any(slot => slot.Item != null);
	}

	/// <summary>
	/// Server only (can be called client side but has no effect).
	/// Add this player to the list of players currently observing all slots in the slot tree
	/// This observer will receive updates as they happen to this slot and will
	/// receieve an update for each slot in the tree immediately as the result
	/// of this method.
	/// </summary>
	/// <param name="observerPlayer"></param>
	public void ServerAddObserverPlayer(GameObject observerPlayer, bool topLevelOnly = false)
	{
		if (!CustomNetworkManager.IsServer) return;

		TrySpawnContents();

		serverObserverPlayers.Add(observerPlayer);

		var slots = topLevelOnly ? GetItemSlots() : GetItemSlotTree();

		foreach (var slot in slots)
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
		if (observerPlayer == null) return;
		if (this == null)
		{
			return;
		}

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
		var rootStorage = GetRootStorageOrPlayer();
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
	/// Drops all items in all slots.
	/// </summary>
	public void ServerDropAll(Vector2? worldDeltaTargetVector = null)
	{
		foreach (var itemSlot in GetItemSlots())
		{
			Inventory.ServerDrop(itemSlot, worldDeltaTargetVector);
		}
	}

	/// <summary>
	/// Drops all items in all slots.
	/// </summary>
	public void ServerDropAllAtWorld(Vector3? DropAtWorld = null)
	{
		Vector2? worldDeltaTargetVector = null;
		if (DropAtWorld != null)
		{
			worldDeltaTargetVector = DropAtWorld - gameObject.AssumedWorldPosServer();
		}
		ServerDropAll(worldDeltaTargetVector);
	}
}
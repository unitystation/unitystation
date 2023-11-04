using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Clothing;
using HealthV2;
using Initialisation;
using Items.Implants.Organs;
using Logs;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using Systems.Storage;

public class DynamicItemStorage : NetworkBehaviour, IOnPlayerRejoin, IOnControlPlayer, IOnPlayerLeaveBody
{
	public PlayerNetworkActions playerNetworkActions;
	public RegisterPlayer registerPlayer;

	//Think of it as basically item storage but It's handy to have the extra data For slots
	public HashSet<IDynamicItemSlotS> ServerContainedInventorys = new HashSet<IDynamicItemSlotS>();
	public HashSet<IDynamicItemSlotS> ClientContainedInventorys = new HashSet<IDynamicItemSlotS>();

	public Dictionary<GameObject, List<ItemSlot>> ClientObjectToSlots = new Dictionary<GameObject, List<ItemSlot>>();
	public Dictionary<NamedSlot, List<ItemSlot>> ClientContents = new Dictionary<NamedSlot, List<ItemSlot>>();
	public List<ItemSlot> ClientTotal = new List<ItemSlot>();


	//ItemSlot to BodyPartUISlots.StorageCharacteristics for How it should act and look
	public Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics> ServerSlotCharacteristic =
		new Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics>();

	public Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics> ClientSlotCharacteristic =
		new Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics>();

	//Good for looking up if you know what the object The slot is on
	public Dictionary<GameObject, List<ItemSlot>> ServerObjectToSlots = new Dictionary<GameObject, List<ItemSlot>>();

	//The main storage method for slots
	public Dictionary<NamedSlot, List<ItemSlot>> ServerContents = new Dictionary<NamedSlot, List<ItemSlot>>();

	//If you would like all of them ItemSlots
	public List<ItemSlot> ServerTotal = new List<ItemSlot>();


	private readonly Dictionary<IDynamicItemSlotS, List<InternalData>> InternalSynchronisingContainedInventorys =
		new Dictionary<IDynamicItemSlotS, List<InternalData>>();


	public List<InternalData> UIBodyPartsToSerialise = new List<InternalData>();

	//Client snapshot so it can tell what changed
	public List<InternalData> ClientUIBodyPartsToSerialise = new List<InternalData>();

	//For conditional stuff so only allow one or require n Slots show stuff
	public Dictionary<string, List<Conditional>> ServerConditionals = new Dictionary<string, List<Conditional>>();
	public Dictionary<string, List<Conditional>> ClientConditionals = new Dictionary<string, List<Conditional>>();

	public Dictionary<string, Conditional> ServerActiveConditional = new Dictionary<string, Conditional>();
	public Dictionary<string, Conditional> ClientActiveConditional = new Dictionary<string, Conditional>();

	public HashSet<GameObject> Observers = new HashSet<GameObject>();

	public string GetSetData => SerialisedNetIDs;


	[SyncVar(hook = nameof(UpdateSlots))] private string SerialisedNetIDs = "";


	private readonly List<InternalData> added = new List<InternalData>();
	private readonly List<InternalData> removed = new List<InternalData>();


	//You should not need to reference the occupation, Best to use mind Instead
	public Occupation InitialisedWithOccupation { get; private set; } = null;

	public class InternalData
	{
		public uint ID;
		public int IndexEnabled;
	}


	private List<ItemSlot> EmptyList = new List<ItemSlot>(0);

	//Because the old system used standard populated
	public PlayerSlotStoragePopulator StandardPopulator;

	/// <summary>
	/// Used for when inventories added or removed on client
	/// </summary>
	public readonly UnityEvent OnContentsChangeClient = new UnityEvent();

	/// <summary>
	/// Used for when inventories added or removed on server
	/// </summary>
	public readonly UnityEvent OnContentsChangeServer = new UnityEvent();

	public void Awake()
	{
		playerNetworkActions = GetComponent<PlayerNetworkActions>();
		registerPlayer = GetComponent<RegisterPlayer>();
		Observers.Add(this.gameObject);
	}

	public HashSet<IDynamicItemSlotS> GetContainedInventorys()
	{
		if (isServer)
		{
			return ServerContainedInventorys;
		}
		else
		{
			return ClientContainedInventorys;
		}
	}


	//Returns the correct content depending on server or client
	public Dictionary<NamedSlot, List<ItemSlot>> GetCorrectContents()
	{
		if (isServer)
		{
			return ServerContents;
		}
		else
		{
			return ClientContents;
		}
	}

	//Returns all slots Including subinventories depending if server or client
	public IEnumerable<ItemSlot> GetItemSlotTree()
	{
		if (isServer)
		{
			return ServerTotal.SelectMany(ItemStorage.SlotSubtree);
		}
		else
		{
			return ClientTotal.SelectMany(ItemStorage.SlotSubtree);
		}
	}

	private List<ItemSlot> GetCorrectTotalSlots(bool client = false)
	{
		if (isServer && client == false)
		{
			return ServerTotal;
		}
		else
		{
			return ClientTotal;
		}
	}

	public IEnumerable<ItemSlot> GetItemSlots(bool client = false)
	{
		if (isServer && client == false)
		{
			return ServerTotal;
		}
		else
		{
			return ClientTotal;
		}
	}

	private Dictionary<string, Conditional> GetActiveConditionals(bool client = false)
	{
		if (isServer && client == false)
		{
			return ServerActiveConditional;
		}
		else
		{
			return ClientActiveConditional;
		}
	}

	private Dictionary<string, List<Conditional>> GetConditionals(bool Client = false)
	{
		if (isServer && Client == false)
		{
			return ServerConditionals;
		}
		else
		{
			return ClientConditionals;
		}
	}

	/// <summary>
	/// Use this if you know the game object that the slot is on
	/// </summary>
	/// <param name="relatedPart"></param>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public ItemSlot GetNamedItemSlot(GameObject relatedPart, NamedSlot namedSlot)
	{
		if (isServer)
		{
			if (ServerObjectToSlots.ContainsKey(relatedPart) == false) return null;
			foreach (var itemSlot in ServerObjectToSlots[relatedPart])
			{
				if (itemSlot.NamedSlot == namedSlot)
				{
					return itemSlot;
				}
			}
		}
		else
		{
			if (ClientObjectToSlots.ContainsKey(relatedPart) == false) return null;
			foreach (var itemSlot in ClientObjectToSlots[relatedPart])
			{
				if (itemSlot.NamedSlot == namedSlot)
				{
					return itemSlot;
				}
			}
		}

		return null;
	}

	/// <summary>
	/// Used for getting all the pocket slots on player
	/// </summary>
	/// <returns></returns>
	public List<ItemSlot> GetPocketsSlots()
	{
		if (isServer)
		{
			List<ItemSlot> Pockets = new List<ItemSlot>();
			foreach (var named in PocketSlots)
			{
				if (ServerContents.ContainsKey(named) == false) continue;
				Pockets.AddRange(ServerContents[named]);
			}

			return Pockets;
		}
		else
		{
			List<ItemSlot> Pockets = new List<ItemSlot>();
			foreach (var named in PocketSlots)
			{
				if (ClientContents.ContainsKey(named) == false) continue;
				Pockets.AddRange(ClientContents[named]);
			}

			return Pockets;
		}
	}


	/// <summary>
	/// Getting the list of all Specified named slots On Storage
	/// </summary>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public List<ItemSlot> GetNamedItemSlots(NamedSlot namedSlot)
	{
		if (isServer)
		{
			if (ServerContents.ContainsKey(namedSlot) == false) return EmptyList;
			return ServerContents[namedSlot];
		}
		else
		{
			if (ClientContents.ContainsKey(namedSlot) == false) return EmptyList;
			return ClientContents[namedSlot];
		}
	}

	/// <summary>
	/// Gets all the hand slots on player though does generate GC
	/// </summary>
	/// <returns></returns>
	public List<ItemSlot> GetHandSlots()
	{
		List<ItemSlot> HandSlots = new List<ItemSlot>();
		if (isServer)
		{
			if (ServerContents.ContainsKey(NamedSlot.leftHand))
			{
				foreach (var itemSlot in ServerContents[NamedSlot.leftHand])
				{
					HandSlots.Add(itemSlot);
				}
			}

			if (ServerContents.ContainsKey(NamedSlot.rightHand))
			{
				foreach (var itemSlot in ServerContents[NamedSlot.rightHand])
				{
					HandSlots.Add(itemSlot);
				}
			}
		}
		else
		{
			if (ClientContents.ContainsKey(NamedSlot.leftHand))
			{
				foreach (var itemSlot in ClientContents[NamedSlot.leftHand])
				{
					HandSlots.Add(itemSlot);
				}
			}

			if (ClientContents.ContainsKey(NamedSlot.rightHand))
			{
				foreach (var itemSlot in ClientContents[NamedSlot.rightHand])
				{
					HandSlots.Add(itemSlot);
				}
			}
		}

		return HandSlots;
	}

	/// <summary>
	/// Use this if you want to bung something into someone's inventory anywhere possible
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public ItemSlot GetBestHandOrSlotFor(GameObject item)
	{
		ItemSlot bestHand = GetBestHand();

		if (bestHand != null)
		{
			return bestHand;
		}

		return GetBestSlotFor(item);
	}

	public ItemSlot GetBestSlotFor(GameObject toCheck)
	{
		if (toCheck == null) return null;
		return GetBestSlotFor(toCheck.GetComponent<Pickupable>());
	}

	public ItemSlot GetBestSlotFor(Pickupable toCheck)
	{
		return BestSlotForTrait.Instance.GetBestSlot(toCheck, this, false);
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
	public static readonly NamedSlot[] PocketSlots =
	{
		NamedSlot.storage01, NamedSlot.storage02, NamedSlot.storage03, NamedSlot.storage04,
		NamedSlot.storage05, NamedSlot.storage06, NamedSlot.storage07, NamedSlot.storage08,
		NamedSlot.storage09, NamedSlot.storage10,
	};

	/// <summary>
	/// Slots gas containers can be used from.
	/// </summary>
	public static readonly NamedSlot[] GasUseSlots =
	{
		NamedSlot.leftHand, NamedSlot.rightHand, NamedSlot.storage01, NamedSlot.storage02, NamedSlot.storage03,
		NamedSlot.storage04,
		NamedSlot.storage05, NamedSlot.storage06, NamedSlot.storage07, NamedSlot.storage08,
		NamedSlot.storage09, NamedSlot.storage10,
		NamedSlot.suitStorage, NamedSlot.back, NamedSlot.belt
	};


	/// <summary>
	/// The item slot representing the active hand. Null if this is not a player.
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetActiveHandSlot()
	{
		if (playerNetworkActions == null) return null;
		if (playerNetworkActions.activeHand == null) return null;
		return GetNamedItemSlot(playerNetworkActions.activeHand, playerNetworkActions.CurrentActiveHand);
	}

	public void PassthroughContentsChangeClient()
	{
		OnContentsChangeClient.Invoke();
	}

	public void PassthroughContentsChangeServer()
	{
		OnContentsChangeServer.Invoke();
	}


	public bool CanAccommodate(ItemSlot ItemSlot, Stackable CheckStackable = null)
	{
		if (ItemSlot.IsEmpty) return true;
		if (CheckStackable != null)
		{
			var Stackable = ItemSlot.Item.GetComponent<Stackable>();
			if (Stackable == null) return false;
			return Stackable.StacksWith(CheckStackable);
		}

		return false;
	}

	/// <summary>
	/// Find the most appropriate Empty hand slot
	/// </summary>
	/// <returns></returns>
	public ItemSlot GetBestHand(Stackable checkStackable = null)
	{
		if (playerNetworkActions == null)
		{
			return default;
		}

		var activeHand = GetNamedItemSlot(playerNetworkActions.activeHand, playerNetworkActions.CurrentActiveHand);
		if (CanAccommodate(activeHand, checkStackable))
		{
			return activeHand;
		}

		var leftHands = GetNamedItemSlots(NamedSlot.leftHand);
		foreach (var leftHand in leftHands)
		{
			if (leftHand != activeHand && CanAccommodate(leftHand, checkStackable))
			{
				return leftHand;
			}
		}

		var rightHands = GetNamedItemSlots(NamedSlot.rightHand);
		foreach (var rightHand in rightHands)
		{
			if (rightHand != activeHand && CanAccommodate(rightHand, checkStackable))
			{
				return rightHand;
			}
		}

		return default;
	}

	/// <summary>
	/// Removes inventory from dynamic storage
	/// </summary>
	/// <param name="bodyPartUISlots"></param>
	[Server]
	public void Remove(IDynamicItemSlotS bodyPartUISlots)
	{
		try
		{
			if (ServerContainedInventorys.Contains(bodyPartUISlots) == false) return;
			bodyPartUISlots.RelatedStorage.ServerRemoveObserverPlayer(this.gameObject);
			ServerContainedInventorys.Remove(bodyPartUISlots);

			if (InternalSynchronisingContainedInventorys.ContainsKey(bodyPartUISlots))
			{
				foreach (var id in InternalSynchronisingContainedInventorys[bodyPartUISlots])
				{
					UIBodyPartsToSerialise.Remove(id);
				}

				InternalSynchronisingContainedInventorys.Remove(bodyPartUISlots);
			}
			foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
			{
				item.OnSlotContentsChangeServer.RemoveListener(PassthroughContentsChangeServer);
				item.OnSlotContentsChangeServer.RemoveListener(PassthroughContentsChangeClient);
			}
		}
		catch (NullReferenceException exception)
		{
			Loggy.LogError($"Caught NRE in DynamicItemStorage.Remove: {exception.Message} \n {exception.StackTrace}",
				Category.Inventory);
			return;
		}


		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			var sstorageCharacteristicse = storageCharacteristicse;
			var bbodyPartUISlots = bodyPartUISlots;
			var slot = bbodyPartUISlots.RelatedStorage.GetNamedItemSlot(sstorageCharacteristicse.namedSlot);
			var Check = CheckConditionalRemove(bodyPartUISlots, sstorageCharacteristicse, slot);
			if (Check.Item1)
			{
				if (Check.Item2 != null)
				{
					sstorageCharacteristicse = Check.Item3;
					bbodyPartUISlots = Check.Item2;
					slot = bbodyPartUISlots.RelatedStorage.GetNamedItemSlot(sstorageCharacteristicse.namedSlot);

					if (InternalSynchronisingContainedInventorys.ContainsKey(bbodyPartUISlots))
					{
						foreach (var ID in InternalSynchronisingContainedInventorys[bbodyPartUISlots])
						{
							if (ID.IndexEnabled == sstorageCharacteristicse.IndexInList)
							{
								UIBodyPartsToSerialise.Remove(ID);
							}
						}
					}
				}
				else
				{
					continue;
				}
			}

			if (ServerContents.ContainsKey(sstorageCharacteristicse.namedSlot) == false)
				ServerContents[sstorageCharacteristicse.namedSlot] = new List<ItemSlot>();

			ServerContents[sstorageCharacteristicse.namedSlot].Remove(slot);

			if (bbodyPartUISlots.GameObject == null)
			{
				continue; //Being destroyed?
			}
			if (ServerObjectToSlots.ContainsKey(bbodyPartUISlots.GameObject))
			{
				ServerObjectToSlots[bbodyPartUISlots.GameObject].Remove(slot);
			}
			else
			{
				Loggy.LogWarning("Key was not found for Body Part UI Slot Object", Category.Inventory);
				continue;
			}

			ServerTotal.Remove(slot);
			if (ServerSlotCharacteristic.ContainsKey(slot)) ServerSlotCharacteristic.Remove(slot);
			if (sstorageCharacteristicse.DropContents)
			{
				Inventory.ServerDrop(slot);
			}

			if (slot.Item != null)
			{
				if (slot.Item.TryGetComponent<ClothingSlots>(out var Clothing))
				{
					Clothing.RemoveSelf(this);
				}
			}
		}

		if (bodyPartUISlots.GameObject != null && ServerObjectToSlots.ContainsKey(bodyPartUISlots.GameObject) &&
		    ServerObjectToSlots[bodyPartUISlots.GameObject].Count == 0)
		{
			ServerObjectToSlots.Remove(bodyPartUISlots.GameObject);
		}

		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(null);


		SerialisedNetIDs = JsonConvert.SerializeObject(UIBodyPartsToSerialise);

		OnContentsChangeServer.Invoke();
	}

	/// <summary>
	/// Adds item storage to dynamic storage
	/// </summary>
	/// <param name="bodyPartUISlots"></param>
	[Server]
	public void Add(IDynamicItemSlotS bodyPartUISlots)
	{
		if (ServerContainedInventorys.Contains(bodyPartUISlots)) return;
		bodyPartUISlots.RelatedStorage.ServerAddObserverPlayer(this.gameObject);
		ServerContainedInventorys.Add(bodyPartUISlots);

		InternalSynchronisingContainedInventorys[bodyPartUISlots] = new List<InternalData>();

		uint id = bodyPartUISlots.GameObject.GetComponent<NetworkIdentity>().netId;


		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(registerPlayer);
		foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
		{
			item.OnSlotContentsChangeServer.AddListener(PassthroughContentsChangeServer);
			item.OnSlotContentsChangeServer.AddListener(PassthroughContentsChangeClient);
		}

		int i = 0;
		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			storageCharacteristicse.RelatedIDynamicItemSlotS = bodyPartUISlots;
			var Slot = bodyPartUISlots.RelatedStorage.GetNamedItemSlot(storageCharacteristicse.namedSlot);

			if (CheckConditionalAdd(bodyPartUISlots, storageCharacteristicse, Slot))
			{
				i++;
				continue;
			}

			if (ServerContents.ContainsKey(storageCharacteristicse.namedSlot) == false)
				ServerContents[storageCharacteristicse.namedSlot] = new List<ItemSlot>();
			ServerContents[storageCharacteristicse.namedSlot].Add(Slot);

			if (ServerObjectToSlots.ContainsKey(bodyPartUISlots.GameObject) == false)
			{
				ServerObjectToSlots.Add(bodyPartUISlots.GameObject, new List<ItemSlot>());
			}


			ServerObjectToSlots[bodyPartUISlots.GameObject].Add(Slot);

			ServerTotal.Add(Slot);
			var InternalData = new InternalData
			{
				ID = id,
				IndexEnabled = i,
			};

			storageCharacteristicse.IndexInList = i;
			InternalSynchronisingContainedInventorys[bodyPartUISlots].Add(InternalData);
			UIBodyPartsToSerialise.Add(InternalData);

			ServerSlotCharacteristic[Slot] = storageCharacteristicse;
			if (Slot.Item != null)
			{
				if (Slot.Item.TryGetComponent<ClothingSlots>(out var Clothing))
				{
					Clothing.AddSelf(this);
				}
			}

			i++;
		}


		SerialisedNetIDs = JsonConvert.SerializeObject(UIBodyPartsToSerialise);
		// if (hasAuthority)
		// {
		// UpdateSlots(SerialisedNetIDs, SerialisedNetIDs);
		// }

		OnContentsChangeServer.Invoke();
	}


	public void AddClient(IDynamicItemSlotS bodyPartUISlots, int index)
	{
		ClientContainedInventorys.Add(bodyPartUISlots);
		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(registerPlayer);
		foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
		{
			item.OnSlotContentsChangeClient.AddListener(PassthroughContentsChangeClient);
		}

		var storageCharacteristicse = bodyPartUISlots.Storage[index];
		storageCharacteristicse.IndexInList = index;


		storageCharacteristicse.RelatedIDynamicItemSlotS = bodyPartUISlots;
		var Slot = bodyPartUISlots.RelatedStorage.GetNamedItemSlot(storageCharacteristicse.namedSlot);


		if (ClientContents.ContainsKey(storageCharacteristicse.namedSlot) == false)
			ClientContents[storageCharacteristicse.namedSlot] = new List<ItemSlot>();
		ClientContents[storageCharacteristicse.namedSlot]
			.Add(Slot);

		if (ClientObjectToSlots.ContainsKey(bodyPartUISlots.GameObject) == false)
			ClientObjectToSlots[bodyPartUISlots.GameObject] = new List<ItemSlot>();
		ClientObjectToSlots[bodyPartUISlots.GameObject]
			.Add(Slot);
		ClientSlotCharacteristic[Slot] = storageCharacteristicse;
		ClientTotal.Add(Slot);

		if (hasAuthority && storageCharacteristicse.NotPresentOnUI == false)
		{
			UIManager.Instance.UI_SlotManager.SetActive(true);
			UIManager.Instance.UI_SlotManager.UpdateUI();
		}


		OnContentsChangeClient.Invoke();
	}

	public void RemoveClient(IDynamicItemSlotS bodyPartUISlots, int index)
	{
		if (bodyPartUISlots == null) return;
		if (ClientContainedInventorys.Contains(bodyPartUISlots))
		{
			ClientContainedInventorys.Remove(bodyPartUISlots);
		}

		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(null);

		foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
		{
			item.OnSlotContentsChangeClient.RemoveListener(PassthroughContentsChangeClient);
		}

		var sstorageCharacteristicse = bodyPartUISlots.Storage[index];
		var BbodyPartUISlots = bodyPartUISlots;
		var slot = BbodyPartUISlots.RelatedStorage.GetNamedItemSlot(sstorageCharacteristicse.namedSlot);

		if (ClientContents.ContainsKey(sstorageCharacteristicse.namedSlot) == false)
			ClientContents[sstorageCharacteristicse.namedSlot] = new List<ItemSlot>();
		ClientContents[sstorageCharacteristicse.namedSlot]
			.Remove(slot);
		if (BbodyPartUISlots.GameObject != null && ClientObjectToSlots.ContainsKey(BbodyPartUISlots.GameObject) == false)
			ClientObjectToSlots[BbodyPartUISlots.GameObject] = new List<ItemSlot>();

		if (BbodyPartUISlots.GameObject != null)
		{
			ClientObjectToSlots[BbodyPartUISlots.GameObject].Remove(slot);
		}

		if (slot != null && ClientSlotCharacteristic.ContainsKey(slot))
		{
			ClientSlotCharacteristic.Remove(slot);
		}

		ClientTotal.Remove(slot);
		if (hasAuthority)
		{
			UIManager.Instance.UI_SlotManager.UpdateUI();
		}

		//UIManager.Instance.UI_SlotManager.RemoveContainer(bodyPartUISlots);
		OnContentsChangeClient.Invoke();
	}

	/// <summary>
	/// Takes the serialised string and deserialises it and works out what's changed on the storage since last updated
	/// </summary>
	/// <param name="oldST"></param>
	/// <param name="NewST"></param>
	public void UpdateSlots(string oldST, string NewST)
	{
		SerialisedNetIDs = NewST;
		ProcessChangeClient(NewST);
	}

	public void ShowClientUI()
	{
		var BackupData = GetSetData;
		if (string.IsNullOrEmpty(BackupData) == false)
		{
			ProcessChangeClient("[]");
			ProcessChangeClient(BackupData);
		}
	}


	private IEnumerator WaitAFrame(string NewST)
	{
		yield return null;
		ProcessChangeClient(NewST);
	}

	private readonly Dictionary<uint, IDynamicItemSlotS> ClientCash = new Dictionary<uint, IDynamicItemSlotS>();

	public void ProcessChangeClient(string NewST, int Tries = 0)
	{
		added.Clear();
		removed.Clear();
		var incomingList = JsonConvert.DeserializeObject<List<InternalData>>(NewST);
		var spawnedList = CustomNetworkManager.IsServer ? NetworkServer.spawned : NetworkClient.spawned;
		if (incomingList != null)
		{
			foreach (var IntIn in incomingList)
			{
				if (spawnedList.TryGetValue(IntIn.ID, out var spawned) == false)
				{
					if (Tries > 10)
					{
						Loggy.LogError($"Failed to find object in spawned objects, might have not spawned yet? netId: {IntIn}");
						continue;
					}
					WeakReference<DynamicItemStorage> wptr = new WeakReference<DynamicItemStorage>(this);

					LoadManager.RegisterActionDelayed(() =>
					{
						DynamicItemStorage DIS;

						int LocalTries = Tries;
						LocalTries++;

						if (wptr.TryGetTarget(out DIS))
						{
							DIS.ProcessChangeClient(NewST, LocalTries);
						}
					}, 30);
					return;
				}


				bool Contain = false;
				foreach (var ID in ClientUIBodyPartsToSerialise)
				{
					if (ID.ID == IntIn.ID && ID.IndexEnabled == IntIn.IndexEnabled)
					{
						Contain = true;
						break;
					}
				}

				if (Contain == false)
				{
					added.Add(IntIn);
				}
			}
		}

		foreach (var IntIn in ClientUIBodyPartsToSerialise)
		{
			bool Contain = false;
			foreach (var ID in incomingList)
			{
				if (ID.ID == IntIn.ID && ID.IndexEnabled == IntIn.IndexEnabled)
				{
					Contain = true;
					break;
				}
			}

			if (Contain == false)
			{
				removed.Add(IntIn);
			}
		}

		foreach (var addInt in removed)
		{
			IDynamicItemSlotS Inspawned = null;
			if (ClientCash.ContainsKey(addInt.ID) == false)
			{
				if (spawnedList.TryGetValue(addInt.ID, out var spawned) == false) //TODO Cash!
				{
					Loggy.LogError(
						$"Failed to find object in spawned objects, might have not spawned yet? netId: {addInt}");
					continue;
				}

				if (spawned == null)
				{
					continue;
				}

				Inspawned = GetComponent<IDynamicItemSlotS>();
			}
			else
			{
				Inspawned = ClientCash[addInt.ID];
			}

			RemoveClient(Inspawned, addInt.IndexEnabled);
		}

		foreach (var addInt in added)
		{
			if (spawnedList.TryGetValue(addInt.ID, out var spawned) == false)
			{
				Loggy.LogError(
					$"Failed to find object in spawned objects, might have not spawned yet? netId: {addInt}");
				continue;
			}

			if (spawned == null)
			{
				continue;
			}

			var InIDynamicItemSlotS = spawned.GetComponent<IDynamicItemSlotS>();
			ClientCash[addInt.ID] = InIDynamicItemSlotS;
			AddClient(InIDynamicItemSlotS, addInt.IndexEnabled);
		}

		if (incomingList != null)
		{
			ClientUIBodyPartsToSerialise = incomingList;
		}

	}

	public void OnDestroy()
	{
		if (isServer)
		{
			foreach (var itemStorage in ServerContainedInventorys.ToArray())
			{
				Remove(itemStorage);
			}
		}
		else
		{

			foreach (var dynamicItemSlot in ClientContainedInventorys.ToArray())
			{
				int i = 0;
				foreach (var SC in dynamicItemSlot.Storage)
				{
					RemoveClient(dynamicItemSlot, i);
					i++;
				}

			}
		}
	}

	/// <summary>
	/// Checks if this game object is present in this storage
	/// </summary>
	/// <param name="TargetObject"></param>
	/// <returns></returns>
	public bool InventoryHasObject(GameObject TargetObject)
	{
		foreach (var itemSlot in GetCorrectTotalSlots())
		{
			if (itemSlot.ItemObject == TargetObject)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks if object is in any of the Specified name slot category
	/// </summary>
	/// <param name="TargetObject"></param>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public bool InventoryHasObjectInCategory(GameObject TargetObject, NamedSlot namedSlot)
	{
		var Contents = GetCorrectContents();
		if (Contents.ContainsKey(namedSlot) == false) return false;

		foreach (var itemSlot in Contents[namedSlot])
		{
			if (itemSlot.ItemObject == TargetObject)
			{
				return true;
			}
		}

		return false;
	}


	/// <summary>
	/// Does checks for conditional slots Such as require n or only allow one
	/// </summary>
	/// <param name="IDynamicItemSlotS"></param>
	/// <param name="storageCharacteristicse"></param>
	/// <param name="Slot"></param>
	/// <param name="client"></param>
	/// <returns></returns>
	public bool CheckConditionalAdd(IDynamicItemSlotS IDynamicItemSlotS,
		BodyPartUISlots.StorageCharacteristics storageCharacteristicse, ItemSlot Slot, bool client = false)
	{
		if (storageCharacteristicse.Conditional)
		{
			var AddSlot = new Conditional()
			{
				BodyPartUISlots = IDynamicItemSlotS,
				StorageCharacteristics = storageCharacteristicse,
				ItemSlot = Slot
			};

			var Conditionals = GetConditionals(client);
			if (storageCharacteristicse.Condition.ConditionalParameter ==
			    BodyPartUISlots.ConditionalParameter.RequireX)
			{
				if (Conditionals.ContainsKey(storageCharacteristicse.Condition.CategoryID) == false)
					Conditionals[storageCharacteristicse.Condition.CategoryID] = new List<Conditional>();
				Conditionals[storageCharacteristicse.Condition.CategoryID].Add(AddSlot);

				if ((Conditionals[storageCharacteristicse.Condition.CategoryID].Count ==
				     storageCharacteristicse.Condition.XAmountConditional) == false)
				{
					return true;
				}

				var Active = GetActiveConditionals(client);
				Active[storageCharacteristicse.Condition.CategoryID] = AddSlot;
			}
			else if (storageCharacteristicse.Condition.ConditionalParameter ==
			         BodyPartUISlots.ConditionalParameter.OnlyAllowOne)
			{
				if (Conditionals.ContainsKey(storageCharacteristicse.Condition.CategoryID) == false)
					Conditionals[storageCharacteristicse.Condition.CategoryID] = new List<Conditional>();
				Conditionals[storageCharacteristicse.Condition.CategoryID].Add(AddSlot);
				if ((Conditionals[storageCharacteristicse.Condition.CategoryID].Count == 1) == false)
				{
					return true;
				}

				var Active = GetActiveConditionals(client);
				Active[storageCharacteristicse.Condition.CategoryID] = AddSlot;
			}
		}

		return false;
	}

	public void SetUpOccupation(Occupation occupation)
	{
		if (occupation == null)
		{
			//TODO: Disable this warning after attributes has completely replaced occupations.
			Loggy.LogWarning($"[DynamicInventory] - Attempted to use a null occupation!");
			return;
		}

		InitialisedWithOccupation = occupation; //Can't really think of anywhere better to put this
		//
		var NSP = occupation.InventoryPopulator as PlayerSlotStoragePopulator;
		if (NSP != null)
		{
			NSP.PopulateDynamicItemStorage(this, registerPlayer.PlayerScript, occupation.UseStandardPopulator);
		}
	}

	public void SetUpFromPopulator(PlayerSlotStoragePopulator providedPopulator)
	{
		providedPopulator.PopulateDynamicItemStorage(this, registerPlayer.PlayerScript);
	}

	#region Check Conditionals

	public Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics> CheckConditionalRemove(
		IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics storageCharacteristicse, ItemSlot Slot)
	{
		if (storageCharacteristicse.Conditional)
		{
			Conditional AddSlot = new Conditional();

			var Conditionals = GetConditionals();
			if (storageCharacteristicse.Condition.ConditionalParameter ==
			    BodyPartUISlots.ConditionalParameter.RequireX)
			{
				if (Conditionals.ContainsKey(storageCharacteristicse.Condition.CategoryID) == false)
					Conditionals[storageCharacteristicse.Condition.CategoryID] = new List<Conditional>();

				foreach (var conditional in Conditionals[storageCharacteristicse.Condition.CategoryID])
				{
					if (conditional.ItemSlot == Slot)
					{
						AddSlot = conditional;
					}
				}

				if (Conditionals[storageCharacteristicse.Condition.CategoryID].Contains(AddSlot))
				{
					Conditionals[storageCharacteristicse.Condition.CategoryID].Remove(AddSlot);
				}

				if (Conditionals[storageCharacteristicse.Condition.CategoryID].Count >=
				    storageCharacteristicse.Condition.XAmountConditional)
				{
					if (GetCorrectTotalSlots().Contains(Slot))
					{
						//Assuming the slots are compatible, They should be since they should be the same thing
						Inventory.ServerTransfer(Slot,
							Conditionals[storageCharacteristicse.Condition.CategoryID][
								storageCharacteristicse.Condition.XAmountConditional - 1].ItemSlot);

						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(false, null,
							null);
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(true, null,
							null);
					}
				}
				else
				{
					var Active = GetActiveConditionals(false);
					if (Active.ContainsKey(storageCharacteristicse.Condition.CategoryID))
					{
						var tuple = new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(true,
							Active[storageCharacteristicse.Condition.CategoryID].BodyPartUISlots,
							Active[storageCharacteristicse.Condition.CategoryID].StorageCharacteristics);
						Active.Remove(storageCharacteristicse.Condition.CategoryID);
						return tuple;
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(true, null,
							null);
					}
				}
			}
			else if (storageCharacteristicse.Condition.ConditionalParameter ==
			         BodyPartUISlots.ConditionalParameter.OnlyAllowOne)
			{
				if (Conditionals.ContainsKey(storageCharacteristicse.Condition.CategoryID) == false)
					Conditionals[storageCharacteristicse.Condition.CategoryID] = new List<Conditional>();

				if (Conditionals[storageCharacteristicse.Condition.CategoryID].Contains(AddSlot))
				{
					Conditionals[storageCharacteristicse.Condition.CategoryID].Remove(AddSlot);
				}

				if (Conditionals[storageCharacteristicse.Condition.CategoryID].Count > 0)
				{
					if (GetCorrectTotalSlots().Contains(Slot))
					{
						//Assuming the slots are compatible, They should be since they should be the same thing
						Inventory.ServerTransfer(Slot,
							Conditionals[storageCharacteristicse.Condition.CategoryID][0].ItemSlot);

						if (false)
						{
							UIManager.Instance.UI_SlotManager.AddIndividual(
								Conditionals[storageCharacteristicse.Condition.CategoryID]
									[0].BodyPartUISlots,
								Conditionals[storageCharacteristicse.Condition.CategoryID]
									[0].StorageCharacteristics);
						}

						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(false, null,
							null);
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(true, null,
							null);
					}
				}
				else
				{
					var Active = GetActiveConditionals();
					if (Active.ContainsKey(storageCharacteristicse.Condition.CategoryID))
					{
						var tuple = new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(true,
							Active[storageCharacteristicse.Condition.CategoryID].BodyPartUISlots,
							Active[storageCharacteristicse.Condition.CategoryID].StorageCharacteristics);
						Active.Remove(storageCharacteristicse.Condition.CategoryID);
						return tuple;
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(true, null,
							null);
					}
				}
			}
		}

		return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics>(false, null,
			null);
	}

	#endregion

	/// <summary>
	/// Ensure players can see inventory changes/Interact with storage
	/// </summary>
	/// <param name="newBody"></param>
	public void ServerAddObserverPlayer(GameObject newBody, bool topLevelOnly = false)
	{
		Observers.Add(newBody);

		foreach (var objt in ServerContainedInventorys)
		{
			objt.RelatedStorage.ServerAddObserverPlayer(newBody, topLevelOnly);
		}
	}

	/// <summary>
	/// Used to ensure that players can no longer see inventory changes/Interact with storage
	/// </summary>
	/// <param name="newBody"></param>
	public void ServerRemoveObserverPlayer(GameObject newBody)
	{
		if (Observers.Contains(newBody))
		{
			Observers.Remove(newBody);
		}

		foreach (var objt in ServerContainedInventorys)
		{
			if (objt == null)
			{
				Loggy.LogError($"ServerObjectToSlots had null key on {gameObject.ExpensiveName()}");
				continue;
			}

			objt.RelatedStorage.ServerRemoveObserverPlayer(newBody);
		}
	}


	/// <summary>
	/// Check if item has an interaction with a an item in a slot
	/// If not or if bool returned is true, swap items
	/// </summary>
	public void TryItemInteract(NamedSlot INslot, bool swapIfEmpty = true)
	{
		var slots = GetNamedItemSlots(INslot);
		foreach (var slot in slots)
		{
			// If full, attempt to interact the two, otherwise swap
			if (slot.Item != null)
			{
				//check IF2 InventoryApply interaction - combine the active hand item with this (only if
				//both are occupied)
				if (TryIF2InventoryApply(slot, slot.Item)) return;

				if (swapIfEmpty)
					SwapItem(slot);
				return;
			}
			else
			{
				if (swapIfEmpty)
					SwapItem(slot);
				return;
			}
		}
	}

	private bool TryIF2InventoryApply(ItemSlot itemSlot, Pickupable Item)
	{
		//check IF2 InventoryApply interaction - apply the active hand item with this (only if
		//target slot is occupied, but it's okay if active hand slot is not occupied)
		if (Item != null)
		{
			var combine = InventoryApply.ByLocalPlayer(itemSlot,
				PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot());
			//check interactables in the active hand (if active hand occupied)
			if (PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item != null)
			{
				var handInteractables = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot().Item
					.GetComponents<IBaseInteractable<InventoryApply>>()
					.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
				if (InteractionUtils.ClientCheckAndTrigger(handInteractables, combine) != null) return true;
			}

			//check interactables in the target
			var targetInteractables = Item.GetComponents<IBaseInteractable<InventoryApply>>()
				.Where(mb => mb != null && (mb as MonoBehaviour).enabled);
			if (InteractionUtils.ClientCheckAndTrigger(targetInteractables, combine) != null) return true;
		}

		return false;
	}

	public bool SwapItem(ItemSlot itemSlot)
	{
		if (HandsController.isValidPlayer())
		{
			var CurrentSlot = PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot();
			if (CurrentSlot != itemSlot)
			{
				if (CurrentSlot.Item == null)
				{
					if (itemSlot.Item != null)
					{
						Inventory.ClientRequestTransfer(itemSlot, CurrentSlot);
						return true;
					}
				}
				else
				{
					if (itemSlot.Item == null)
					{
						Inventory.ClientRequestTransfer(CurrentSlot, itemSlot);
						return true;
					}
				}
			}
		}

		return false;
	}


	public void OnPlayerRejoin(Mind mind)
	{
		//Trigger IOnPlayerRejoin for all items in player inventory
		foreach (var itemSlot in GetItemSlotTree())
		{
			if(itemSlot.IsEmpty) continue;

			var playerRejoins = itemSlot.ItemObject.GetComponents<IOnPlayerRejoin>();
			foreach (var playerRejoin in playerRejoins)
			{
				playerRejoin.OnPlayerRejoin(mind);
			}
		}

		//Gets all the Storage game objects that make up the dynamic item storage
		var InventoryObjects = GetContainedInventorys();

		foreach (var InventoryObject in InventoryObjects )
		{
			var playerRejoins = InventoryObject.GameObject.GetComponents<IOnPlayerRejoin>();
			foreach (var playerRejoin in playerRejoins)
			{
				playerRejoin.OnPlayerRejoin(mind);
			}
		}
	}

	public void OnServerPlayerTransfer(PlayerInfo Account)
	{
		//Trigger IOnPlayerTransfer for all items in player inventory
		foreach (var itemSlot in GetItemSlotTree())
		{
			if(itemSlot.IsEmpty) continue;

			var playerTransfers = itemSlot.ItemObject.GetComponents<IOnControlPlayer>();
			foreach (var playerTransfer in playerTransfers)
			{
				playerTransfer.OnServerPlayerTransfer(Account);
			}
		}

		//Gets all the Storage game objects that make up the dynamic item storage
		var InventoryObjects = GetContainedInventorys();

		foreach (var InventoryObject in InventoryObjects )
		{
			var playerRejoins = InventoryObject.GameObject.GetComponents<IOnControlPlayer>();
			foreach (var playerRejoin in playerRejoins)
			{
				playerRejoin.OnServerPlayerTransfer(Account);
			}
		}
	}

	public void OnPlayerLeaveBody(PlayerInfo Account)
	{
		//Trigger IOnPlayerLeaveBody for all items in top level player inventory
		foreach (var itemSlot in GetItemSlotTree())
		{
			if(itemSlot.IsEmpty) continue;

			var playerLeaveBodies = itemSlot.ItemObject.GetComponents<IOnPlayerLeaveBody>();
			foreach (var playerLeaveBody in playerLeaveBodies)
			{
				playerLeaveBody.OnPlayerLeaveBody(Account);
			}
		}

		//Gets all the Storage game objects that make up the dynamic item storage
		var InventoryObjects = GetContainedInventorys();

		foreach (var InventoryObject in InventoryObjects )
		{
			if (InventoryObject.GameObject == null) continue;
			var playerRejoins = InventoryObject.GameObject.GetComponents<IOnPlayerLeaveBody>();
			foreach (var playerRejoin in playerRejoins)
			{
				playerRejoin.OnPlayerLeaveBody(Account);
			}
		}
	}

	[Server]
	public void ServerDropItemsInHand()
	{
		foreach (var itemSlot in GetNamedItemSlots(NamedSlot.leftHand))
		{
			Inventory.ServerDrop(itemSlot);
		}

		foreach (var itemSlot in GetNamedItemSlots(NamedSlot.rightHand))
		{
			Inventory.ServerDrop(itemSlot);
		}
	}


	public struct Conditional
	{
		public IDynamicItemSlotS BodyPartUISlots;
		public BodyPartUISlots.StorageCharacteristics StorageCharacteristics;
		public ItemSlot ItemSlot;
	}
}
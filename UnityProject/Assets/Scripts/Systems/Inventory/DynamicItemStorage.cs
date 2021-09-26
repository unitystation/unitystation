using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Clothing;
using HealthV2;
using Initialisation;
using Mirror;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;

public class DynamicItemStorage : NetworkBehaviour
{
	public PlayerNetworkActions playerNetworkActions;
	public RegisterPlayer registerPlayer;

	//Think of it as basically item storage but It's handy to have the extra data For slots
	public List<IDynamicItemSlotS> ContainedInventorys = new List<IDynamicItemSlotS>();

	public Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics> ClientSlotCharacteristic =
		new Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics>();

	public Dictionary<GameObject, List<ItemSlot>> ClientObjectToSlots = new Dictionary<GameObject, List<ItemSlot>>();
	public Dictionary<NamedSlot, List<ItemSlot>> ClientContents = new Dictionary<NamedSlot, List<ItemSlot>>();
	public List<ItemSlot> ClientTotal = new List<ItemSlot>();


	//ItemSlot to BodyPartUISlots.StorageCharacteristics for How it should act and look
	public Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics> ServerSlotCharacteristic =
		new Dictionary<ItemSlot, BodyPartUISlots.StorageCharacteristics>();

	//Good for looking up if you know what the object The slot is on
	public Dictionary<GameObject, List<ItemSlot>> ServerObjectToSlots = new Dictionary<GameObject, List<ItemSlot>>();
	//The main storage method for slots
	public Dictionary<NamedSlot, List<ItemSlot>> ServerContents = new Dictionary<NamedSlot, List<ItemSlot>>();
	//If you would like all of them ItemSlots
	public List<ItemSlot> ServerTotal = new List<ItemSlot>();

	//the nedIDs of the Objects the Dynamic storage contains
	public List<uint> UIBodyPartsToSerialise = new List<uint>();
	//Client snapshot so it can tell what changed
	public List<uint> ClientUIBodyPartsToSerialise = new List<uint>();

	//For conditional stuff so only allow one or require n Slots show stuff
	public Dictionary<string, List<Conditional>> ServerConditionals = new Dictionary<string, List<Conditional>>();
	public Dictionary<string, List<Conditional>> ClientConditionals = new Dictionary<string, List<Conditional>>();

	public Dictionary<string, Conditional> ServerActiveConditional = new Dictionary<string, Conditional>();
	public Dictionary<string, Conditional> ClientActiveConditional = new Dictionary<string, Conditional>();

	public HashSet<GameObject> Observers = new HashSet<GameObject>();

	public string GetSetData => SerialisedNetIDs;


	[SyncVar(hook = nameof(UpdateSlots))] private string SerialisedNetIDs = "";

	private List<uint> added = new List<uint>();
	private List<uint> removed = new List<uint>();


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

	//Returns all slots depending if server or client
	public IEnumerable<ItemSlot> GetItemSlotTree()
	{
		if (isServer)
		{
			return ServerTotal;
		}
		else
		{
			return ClientTotal;
		}
	}

	private List<ItemSlot> GetCorrectTotalSlots(bool Client = false)
	{
		if (isServer && Client == false)
		{
			return ServerTotal;
		}
		else
		{
			return ClientTotal;
		}
	}

	public IEnumerable<ItemSlot> GetItemSlots(bool Client = false)
	{
		if (isServer && Client == false)
		{
			return ServerTotal;
		}
		else
		{
			return ClientTotal;
		}
	}

	private Dictionary<string, Conditional> GetActiveConditionals(bool Client = false)
	{
		if (isServer && Client == false)
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
	/// <param name="RelatedPart"></param>
	/// <param name="namedSlot"></param>
	/// <returns></returns>
	public ItemSlot GetNamedItemSlot(GameObject RelatedPart, NamedSlot namedSlot)
	{
		if (isServer)
		{
			if (ServerObjectToSlots.ContainsKey(RelatedPart) == false) return null;
			foreach (var itemSlot in ServerObjectToSlots[RelatedPart])
			{
				if (itemSlot.NamedSlot == namedSlot)
				{
					return itemSlot;
				}
			}
		}
		else
		{
			if (ClientObjectToSlots.ContainsKey(RelatedPart) == false) return null;
			foreach (var itemSlot in ClientObjectToSlots[RelatedPart])
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
		NamedSlot.leftHand, NamedSlot.rightHand, NamedSlot.storage01, NamedSlot.storage02, NamedSlot.storage03, NamedSlot.storage04,
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
	public ItemSlot GetBestHand(Stackable CheckStackable = null)
	{
		if (playerNetworkActions == null)
		{
			return default;
		}

		var activeHand = GetNamedItemSlot(playerNetworkActions.activeHand, playerNetworkActions.CurrentActiveHand);
		if (CanAccommodate(activeHand, CheckStackable))
		{
			return activeHand;
		}

		var leftHands = GetNamedItemSlots(NamedSlot.leftHand);
		foreach (var leftHand in leftHands)
		{
			if (leftHand != activeHand && CanAccommodate(leftHand, CheckStackable))
			{
				return leftHand;
			}
		}

		var rightHands = GetNamedItemSlots(NamedSlot.rightHand);
		foreach (var rightHand in rightHands)
		{
			if (rightHand != activeHand && CanAccommodate(rightHand, CheckStackable))
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
			if (ContainedInventorys.Contains(bodyPartUISlots) == false) return;
			bodyPartUISlots.RelatedStorage.ServerRemoveObserverPlayer(this.gameObject);
			ContainedInventorys.Remove(bodyPartUISlots);
			UIBodyPartsToSerialise.Remove(bodyPartUISlots.GameObject.GetComponent<NetworkIdentity>().netId);
			bodyPartUISlots.RelatedStorage.ServerInventoryItemSlotSet -= InventoryChange;

			foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
			{
				item.OnSlotContentsChangeServer.RemoveListener(PassthroughContentsChangeServer);
				item.OnSlotContentsChangeServer.RemoveListener(PassthroughContentsChangeClient);
			}

		}
		catch (NullReferenceException exception)
		{
			Logger.LogError("Caught NRE in DynamicItemStorage.Remove: " + exception.Message, Category.Inventory);
			return;
		}


		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			var SstorageCharacteristicse = storageCharacteristicse;
			var BbodyPartUISlots = bodyPartUISlots;
			var Slot = BbodyPartUISlots.RelatedStorage.GetNamedItemSlot(SstorageCharacteristicse.namedSlot);
			var Check = CheckConditionalRemove(bodyPartUISlots, SstorageCharacteristicse, Slot, true);
			if (Check.Item1)
			{
				if (Check.Item2 != null)
				{
					SstorageCharacteristicse =
						Check.Item3.GetValueOrDefault(new BodyPartUISlots.StorageCharacteristics());
					BbodyPartUISlots = Check.Item2;
					Slot = BbodyPartUISlots.RelatedStorage.GetNamedItemSlot(SstorageCharacteristicse.namedSlot);
				}
				else
				{
					continue;
				}
			}

			if (ServerContents.ContainsKey(SstorageCharacteristicse.namedSlot) == false)
				ServerContents[SstorageCharacteristicse.namedSlot] = new List<ItemSlot>();

			ServerContents[SstorageCharacteristicse.namedSlot].Remove(Slot);

			if (ServerObjectToSlots.ContainsKey(BbodyPartUISlots.GameObject))
			{
				ServerObjectToSlots[BbodyPartUISlots.GameObject].Remove(Slot);
			}
			else
			{
				Logger.LogWarning("Key was not found for Body Part UI Slot Object", Category.Inventory);
				continue;
			}

			ServerTotal.Remove(Slot);
			if (ServerSlotCharacteristic.ContainsKey(Slot)) ServerSlotCharacteristic.Remove(Slot);
			if (SstorageCharacteristicse.DropContents)
			{
				Inventory.ServerDrop(Slot);
			}

			if (Slot.Item != null)
			{
				if (Slot.Item.TryGetComponent<ClothingSlots>(out var Clothing))
				{
					Clothing.RemoveSelf(this);
				}
			}
		}

		if (ServerObjectToSlots.ContainsKey(bodyPartUISlots.GameObject) && ServerObjectToSlots[bodyPartUISlots.GameObject].Count == 0)
		{
			ServerObjectToSlots.Remove(bodyPartUISlots.GameObject);
		}

		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(null);


		SerialisedNetIDs = JsonConvert.SerializeObject(UIBodyPartsToSerialise);
		// if (isLocalPlayer)
		// {
			// UpdateSlots(SerialisedNetIDs, SerialisedNetIDs);
		// }
		OnContentsChangeServer.Invoke();
	}

	/// <summary>
	/// Adds item storage to dynamic storage
	/// </summary>
	/// <param name="bodyPartUISlots"></param>
	[Server]
	public void Add(IDynamicItemSlotS bodyPartUISlots)
	{
		try
		{
			if (ContainedInventorys.Contains(bodyPartUISlots)) return;
			bodyPartUISlots.RelatedStorage.ServerAddObserverPlayer(this.gameObject);
			ContainedInventorys.Add(bodyPartUISlots);
			UIBodyPartsToSerialise.Add(bodyPartUISlots.GameObject.GetComponent<NetworkIdentity>().netId);
			SerialisedNetIDs = JsonConvert.SerializeObject(UIBodyPartsToSerialise);
			bodyPartUISlots.RelatedStorage.SetRegisterPlayer(registerPlayer);

			bodyPartUISlots.RelatedStorage.ServerInventoryItemSlotSet += InventoryChange;
			foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
			{
				item.OnSlotContentsChangeServer.AddListener(PassthroughContentsChangeServer);
				item.OnSlotContentsChangeServer.AddListener(PassthroughContentsChangeClient);
			}
		}
		catch (NullReferenceException exception)
		{
			Logger.LogError("Caught NRE in DynamicItemStorage.Add: " + exception.Message, Category.Inventory);
			return;
		}

		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			var Slot = bodyPartUISlots.RelatedStorage.GetNamedItemSlot(storageCharacteristicse.namedSlot);

			if (CheckConditionalAdd(bodyPartUISlots, storageCharacteristicse, Slot))
			{
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
			ServerSlotCharacteristic[Slot] = storageCharacteristicse;
			if (Slot.Item != null)
			{
				if (Slot.Item.TryGetComponent<ClothingSlots>(out var Clothing))
				{
					Clothing.AddSelf(this);
				}
			}
		}

		// if (isLocalPlayer)
		// {
			// UpdateSlots(SerialisedNetIDs, SerialisedNetIDs);
		// }

		OnContentsChangeServer.Invoke();
	}


	public void AddClient(IDynamicItemSlotS bodyPartUISlots)
	{
		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(registerPlayer);
		foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
		{
			item.OnSlotContentsChangeClient.AddListener(PassthroughContentsChangeClient);
		}

		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			var Slot = bodyPartUISlots.RelatedStorage.GetNamedItemSlot(storageCharacteristicse.namedSlot);
			if (CheckConditionalAdd(bodyPartUISlots, storageCharacteristicse, Slot, true))
			{
				continue;
			}

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

			if (PlayerManager.LocalPlayer == this.gameObject && storageCharacteristicse.NotPresentOnUI == false)
			{
				UIManager.Instance.UI_SlotManager.SetActive(true);
				UIManager.Instance.UI_SlotManager.AddIndividual(bodyPartUISlots, storageCharacteristicse);
			}
		}


		OnContentsChangeClient.Invoke();
	}

	public void RemoveClient(IDynamicItemSlotS bodyPartUISlots)
	{
		bodyPartUISlots.RelatedStorage.SetRegisterPlayer(null);

		foreach (var item in bodyPartUISlots.RelatedStorage.GetItemSlots())
		{
			item.OnSlotContentsChangeClient.RemoveListener(PassthroughContentsChangeClient);
		}

		foreach (var storageCharacteristicse in bodyPartUISlots.Storage)
		{
			var SstorageCharacteristicse = storageCharacteristicse;
			var BbodyPartUISlots = bodyPartUISlots;
			var Slot = BbodyPartUISlots.RelatedStorage.GetNamedItemSlot(SstorageCharacteristicse.namedSlot);
			var Check = CheckConditionalRemove(bodyPartUISlots, SstorageCharacteristicse, Slot, true);
			if (Check.Item1)
			{
				if (Check.Item2 != null)
				{
					SstorageCharacteristicse =
						Check.Item3.GetValueOrDefault(new BodyPartUISlots.StorageCharacteristics());
					BbodyPartUISlots = Check.Item2;
					Slot = BbodyPartUISlots.RelatedStorage.GetNamedItemSlot(SstorageCharacteristicse.namedSlot);
				}
				else
				{
					continue;
				}
			}

			if (ClientContents.ContainsKey(SstorageCharacteristicse.namedSlot) == false)
				ClientContents[SstorageCharacteristicse.namedSlot] = new List<ItemSlot>();
			ClientContents[SstorageCharacteristicse.namedSlot]
				.Remove(Slot);
			if (ClientObjectToSlots.ContainsKey(BbodyPartUISlots.GameObject) == false)
				ClientObjectToSlots[BbodyPartUISlots.GameObject] = new List<ItemSlot>();
			ClientObjectToSlots[BbodyPartUISlots.GameObject]
				.Remove(Slot);
			if (ClientSlotCharacteristic.ContainsKey(Slot)) ClientSlotCharacteristic.Remove(Slot);
			ClientTotal.Remove(Slot);
			if (PlayerManager.LocalPlayer == this.gameObject)
			{
				UIManager.Instance.UI_SlotManager.RemoveSpecifyedUISlot(BbodyPartUISlots, SstorageCharacteristicse);
			}
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

	public void ProcessChangeClient(string NewST)
	{
		added.Clear();
		removed.Clear();
		var incomingList = JsonConvert.DeserializeObject<List<uint>>(NewST);
		foreach (var IntIn in incomingList)
		{
			if (NetworkIdentity.spawned.TryGetValue(IntIn, out var spawned) == false)
			{
				void TempFunction()
				{
					ProcessChangeClient(NewST);
				}

				LoadManager.RegisterActionDelayed(TempFunction, 30);
				return;
			}

			if (ClientUIBodyPartsToSerialise.Contains(IntIn) == false)
			{
				added.Add(IntIn);
			}
		}

		foreach (var IntIn in ClientUIBodyPartsToSerialise)
		{
			if (incomingList.Contains(IntIn) == false)
			{
				removed.Add(IntIn);
			}
		}

		foreach (var addInt in removed)
		{
			if (NetworkIdentity.spawned.TryGetValue(addInt, out var spawned) == false)
			{
				Logger.LogError($"Failed to find object in spawned objects, might have not spawned yet? netId: {addInt}");
				continue;
			}

			RemoveClient(spawned.GetComponent<IDynamicItemSlotS>());
		}

		foreach (var addInt in added)
		{
			if (NetworkIdentity.spawned.TryGetValue(addInt, out var spawned) == false)
			{
				Logger.LogError($"Failed to find object in spawned objects, might have not spawned yet? netId: {addInt}");
				continue;
			}

			AddClient(spawned.GetComponent<IDynamicItemSlotS>());
		}

		ClientUIBodyPartsToSerialise = incomingList;
	}

	public void OnDestroy()
	{
		if (isServer)
		{
			foreach (var itemStorage in ContainedInventorys.ToArray())
			{
				Remove(itemStorage);
			}
		}
		else
		{
			var newl = new List<uint>();
			ProcessChangeClient(JsonConvert.SerializeObject(newl));
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
		var NSP = occupation.InventoryPopulator as PlayerSlotStoragePopulator;
		if (NSP != null)
		{
			NSP.PopulateDynamicItemStorage(this, registerPlayer.PlayerScript);
		}
	}

	#region check conditionals

	public Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?> CheckConditionalRemove(
		IDynamicItemSlotS bodyPartUISlots,
		BodyPartUISlots.StorageCharacteristics storageCharacteristicse, ItemSlot Slot, bool client = false)
	{
		if (storageCharacteristicse.Conditional)
		{
			Conditional AddSlot = new Conditional();

			var Conditionals = GetConditionals(client);
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

						if (client)
						{
							UIManager.Instance.UI_SlotManager.AddIndividual(
								Conditionals[storageCharacteristicse.Condition.CategoryID]
									[storageCharacteristicse.Condition.XAmountConditional - 1].BodyPartUISlots,
								Conditionals[storageCharacteristicse.Condition.CategoryID]
									[storageCharacteristicse.Condition.XAmountConditional - 1].StorageCharacteristics);
						}

						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(false, null,
							null);
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(true, null,
							null);
					}
				}
				else
				{
					var Active = GetActiveConditionals(client);
					if (Active.ContainsKey(storageCharacteristicse.Condition.CategoryID))
					{
						var tuple = new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(true,
							Active[storageCharacteristicse.Condition.CategoryID].BodyPartUISlots,
							Active[storageCharacteristicse.Condition.CategoryID].StorageCharacteristics);
						 Active.Remove(storageCharacteristicse.Condition.CategoryID);
						 return tuple;
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(true, null,
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
					if (GetCorrectTotalSlots(client).Contains(Slot))
					{
						//Assuming the slots are compatible, They should be since they should be the same thing
						Inventory.ServerTransfer(Slot,
							Conditionals[storageCharacteristicse.Condition.CategoryID][0].ItemSlot);

						if (client)
						{
							UIManager.Instance.UI_SlotManager.AddIndividual(
								Conditionals[storageCharacteristicse.Condition.CategoryID]
									[0].BodyPartUISlots,
								Conditionals[storageCharacteristicse.Condition.CategoryID]
									[0].StorageCharacteristics);
						}

						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(false, null,
							null);
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(true, null,
							null);
					}
				}
				else
				{
					var Active = GetActiveConditionals(client);
					if (Active.ContainsKey(storageCharacteristicse.Condition.CategoryID))
					{
						var tuple = new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(true,
							Active[storageCharacteristicse.Condition.CategoryID].BodyPartUISlots,
							Active[storageCharacteristicse.Condition.CategoryID].StorageCharacteristics);
						Active.Remove(storageCharacteristicse.Condition.CategoryID);
						return tuple;
					}
					else
					{
						return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(true, null,
							null);
					}
				}
			}
		}

		return new Tuple<bool, IDynamicItemSlotS, BodyPartUISlots.StorageCharacteristics?>(false, null,
			null);
	}

	#endregion


	private void InventoryChange(Pickupable RemovedObject, Pickupable AddedObject)
	{
		if (AddedObject != null)
		{
			var ItemStorage = AddedObject.GetComponent<InteractableStorage>()?.ItemStorage;
			if (ItemStorage != null)
			{
				foreach (var Observer in Observers)
				{
					ItemStorage.ServerAddObserverPlayer(Observer);
				}
			}


		}

		if (RemovedObject != null)
		{
			var ItemStorage = RemovedObject.GetComponent<InteractableStorage>()?.ItemStorage;
			if (ItemStorage != null)
			{
				foreach (var Observer in Observers)
				{
					ItemStorage.ServerRemoveObserverPlayer(Observer);
				}
			}

		}

	}

	/// <summary>
	/// Ensure players can see inventory changes/Interact with storage
	/// </summary>
	/// <param name="newBody"></param>
	public void ServerAddObserverPlayer(GameObject newBody)
	{
		Observers.Add(newBody);

		foreach (var objt in ContainedInventorys)
		{
			objt.RelatedStorage.ServerAddObserverPlayer(newBody);
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

		foreach (var objt in ServerObjectToSlots.Keys)
		{
			if (objt == null)
			{
				Logger.LogError($"ServerObjectToSlots had null key on {gameObject.ExpensiveName()}");
				continue;
			}

			if (objt.TryGetComponent<ItemStorage>(out var itemStorage) == false)
			{
				Logger.LogError($"ServerObjectToSlots on {gameObject.ExpensiveName()} had a game object key: {objt.ExpensiveName()} without an ItemStorage ");
				continue;
			}

			itemStorage.ServerRemoveObserverPlayer(newBody);
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
			var combine = InventoryApply.ByLocalPlayer(itemSlot, PlayerManager.LocalPlayerScript.DynamicItemStorage.GetActiveHandSlot());
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



	public struct Conditional
	{
		public IDynamicItemSlotS BodyPartUISlots;
		public BodyPartUISlots.StorageCharacteristics StorageCharacteristics;
		public ItemSlot ItemSlot;
	}
}

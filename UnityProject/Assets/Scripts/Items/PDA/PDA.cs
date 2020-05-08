using System;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(ItemStorage))]
[RequireComponent(typeof(PlayerLightControl))]
public class PDA : NetworkBehaviour, ICheckedInteractable<HandApply>
{
	//MessengerSyncDictionary pdas;

	//The actual list of access allowed set via the server and synced to all clients
	private readonly SyncListInt accessSyncList = new SyncListInt();

	// Check to see if the PDA has been registered the first time
	private bool firstRegister = true;

	// The light the pda controls
	[Tooltip("The light the pda controls")]
	public PlayerLightControl flashlight;

	// Is the flashlight on?
	[NonSerialized] public bool FlashlightOn;

	// The ID that's inserted, if any.
	[NonSerialized] public IDCard IdCard;

	[NonSerialized] public bool IdEject;

	// The slot the ID is currently stored in
	[NonSerialized] public ItemSlot IdSlot = null;

	[FormerlySerializedAs("OnServerIDCardChanged")] [FormerlySerializedAs("IdEvent")]
	public IDEvent onServerIdCardChanged = new IDEvent();

	private GameObject pda;

	[Tooltip("The cartridge loaded into the PDA")]
	public GameObject pdaCartridge;

	private NetworkIdentity pdaId;

	// The anme of the first person who put their ID into the PDA
	[NonSerialized] public string PdaRegisteredName;

	//Local storage of the PDA
	[NonSerialized] private ItemStorage storage;

	[Tooltip(" To unlock or lock the PDA even if the string is given")]
	public bool uplinkLocked;

	// The string that must be entered into the ringtone slot
	[Tooltip("Default uplink string here, only for testing reasons")]
	public string uplinkString;

	[Tooltip("The amount of telecrystals in the PDA")]
	public int teleCrystals = 0;

	//Checks weather the player is trying to insert a cartridge or a new ID
	public bool WillInteract(HandApply interaction, NetworkSide side)
	{
		var hand = interaction.HandObject != null ? interaction.HandObject : null;
		if (hand != null)
			if (hand.GetComponent<PDACartridge>() != null)
			{
				IdEject = false;
				return true;
			}

		if (!DefaultWillInteract.Default(interaction, side))
			return false;

		if (!Validations.HasComponent<IDCard>(interaction.HandObject))
			return false;

		if (!Validations.CanFit(IdSlot, interaction.HandObject, side, true))
			return false;

		IdEject = true;
		return true;
	}
	/// <summary>
	/// Checks weather you're putting a new ID in or trying to put in a cartridge
	/// </summary>
	public void ServerPerformInteraction(HandApply interaction)
	{
		//Eject existing id card if there is one and put new one in
		var hand = interaction.HandObject != null ? interaction.HandObject : null;
		if ((IdSlot.Item != null) & IdEject)
		{
			RemoveDevice(true);
		}
		else if (hand != null && (hand != null) & (hand.GetComponent<PDACartridge>() != null))
		{
			//despawns cartridge and also store it in the PDA's memory
			pdaCartridge = hand;
			Inventory.ServerDespawn(hand);
		}

		Inventory.ServerTransfer(interaction.HandSlot, IdSlot);
	}

	//private MessengerManager messengerSystem;

	/// <summary>
	/// Grabs the components on the game objects and readies itself for slot changes
	/// </summary>
	private void Start()
	{
		//Checks to see if anything is in the item slots and allocate them to their respective variables
		storage = gameObject.GetComponent<ItemStorage>();
		var slot = storage.GetIndexedItemSlot(0);
		if (slot.IsEmpty != true) IdCard = slot.Item.GetComponent<IDCard>();
		slot.OnSlotContentsChangeServer.AddListener(SlotChange);
		//messengerSystem = GameObject.Find("MessengerManager").GetComponent<MessengerManager>();
		pdaId = gameObject.GetComponent<NetworkIdentity>();
		//AddSelf();
	}
	//The methods bellow are general functions of the PDA

	/// <summary>
	/// Resets PDA and tells the MessengerManager to set PDA to unknown
	/// </summary>
	public void PdaReset()
	{
		ReplaceName(pdaId, "UnknownPDA");
		PdaRegisteredName = null;
		firstRegister = true;
	}

	/// <summary>
	/// Locks the uplink
	/// </summary>
	[Server]
	public void LockUplink()
	{
		if (isServer) uplinkLocked = true;
	}
	/// <summary>
	/// Return true if it is server and uplink and notification string are the same
	/// </summary>
	[Server]
	public bool ActivateUplink(string notificationString)
	{
		return isServer && notificationString == uplinkString;
	}

	/// <summary>
	/// Warning! This method could be exploited, do not use in public till I implement security checks
	/// </summary>
	[Server]
	public void SpawnUplinkItem(GameObject objectRequested, int cost)
	{
		if (cost <= teleCrystals)
		{
			Spawn.ServerPrefab(objectRequested, gameObject.WorldPosServer(), count: 1);
		}
	}

	/// <summary>
	/// Toggles the flashlight
	/// </summary>
	public void ToggleFlashlight()
	{
		if (FlashlightOn)
		{
			flashlight.Toggle(false);
			FlashlightOn = false;
		}
		else
		{
			flashlight.Toggle(true);
			FlashlightOn = true;
		}
	}


	/// Checks to see what the new ID is and will register the PDA with the ID if this is its first ID
	private void SlotChange()
	{
		//Checks the slots again and will update the variables
		var slot = storage.GetIndexedItemSlot(0);
		if (slot.IsEmpty != true && isServer)
		{
			IdCard = slot.Item.GetComponent<IDCard>();
			onServerIdCardChanged.Invoke(IdCard);
		}

		//Will register the PDA to the ID card if firstRegister is true
		if (!(firstRegister & (IdCard != null))) return;
		PdaRegisteredName = IdCard.RegisteredName;
		firstRegister = false;
		ReplaceName(pdaId, PdaRegisteredName);
	}

	//Just to make sure this only runs on client as Cmd only works Client ---> server
	[Client]
	private void ReplaceName(NetworkIdentity pdaId, string newName)
	{
		//messengerSystem.ReplaceName(pdaID, newName);
	}

	/// <summary>
	/// A simple check to see if the ID should be ejected or the pda cartridge should be ejected
	/// </summary>
	public void RemoveDevice(bool isId)
	{
		if (isId)
		{
			Inventory.ServerDrop(storage.GetIndexedItemSlot(0));
		}
		else
		{
			Spawn.ServerPrefab(pdaCartridge, gameObject.RegisterTile().WorldPosition, transform.parent, count: 1);
			pdaCartridge = null;
		}
	}

	//The methods below handle any PDA messages that get sent to this PDA, not being used
	/*
	[Client]
	//This only runs once and it's to tell the MessengerManager that this PDA exists

	private void AddSelf()
	{
		Profiler.BeginSample("Addself");
		string name = pdaRegisteredName;
		if(name == null)
		{
			name = "Unknown PDA";
		}
		messengerSystem.AddPDA(pdaID, name);
		Profiler.EndSample();

	}

	[Client]
	/// <summary>
	/// This method handles any messages directed at this PDA, it will check who sent the message (with their NetID)
	/// give them a name, then pass it onto the MessageHandler
	/// </summary>
	public void ReceiveMessage(NetworkIdentity id, string message)
	{
		if (pdas.ContainsKey(id))
		{
			string name = pdas[id];
			// put method here
		}
	}

	public void SendMessage(NetworkIdentity receiver, NetworkIdentity sender, string message)
	{
		messengerSystem.SendMessage(receiver, sender, message);
	}
	void IServerDespawn.OnDespawnServer(DespawnInfo info)
	{
		Debug.Log("removing from dictionary");
		messengerSystem.RemovePDA(pdaID);
	}
	*/
	// All these methods handle ID card access, shoudl only be ran server side because we cant trust client
	[Server]
	public bool HasAccess(Access access)
	{
		return accessSyncList.Contains((int) access);
	}

	[Server]
	public SyncListInt AccessList()
	{
		return accessSyncList;
	}


	// Removes the indicated access from this IDCard
	[Server]
	public void ServerRemoveAccess(Access access)
	{
		if (!HasAccess(access)) return;
		accessSyncList.Remove((int) access);
	}


	// Adds the indicated access to this IDCard
	[Server]
	public void ServerAddAccess(Access access)
	{
		if (HasAccess(access)) return;
		accessSyncList.Add((int) access);
	}
}
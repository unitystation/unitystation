using System;
using Antagonists;
using Mirror;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;


namespace Items.PDA
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(ItemLightControl))]
	[RequireComponent(typeof(HasNetworkTabItem))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(PDANotesNetworkHandler))]
	public class PDALogic : NetworkBehaviour, ICheckedInteractable<HandApply>
	{
		// for messenger system
		//MessengerSyncDictionary pdas;

		//The actual list of access allowed set via the server and synced to all clients
		private readonly SyncListInt accessSyncList = new SyncListInt();

		// Check to see if the PDA has been registered the first time
		private bool firstRegister = true;

		// The light the pda controls
		[Tooltip("The light the pda controls")]
		public ItemLightControl flashlight;

		// Is the flashlight on?
		[NonSerialized]
		public bool FlashlightOn = true;

		// The ID that's inserted, if any.
		[NonSerialized]
		public IDCard IdCard;


		[NonSerialized]
		public bool IdEject;

		// The slot the ID is currently stored in
		[NonSerialized]
		public ItemSlot IdSlot = null;

		[FormerlySerializedAs("OnServerIDCardChanged")] [FormerlySerializedAs("IdEvent")]
		public IDEvent onServerIdCardChanged = new IDEvent();

		//The cartridge loaded into the PDA, acts like a container of methods
		[Tooltip("The cartridge loaded into the PDA")]
		public GameObject pdaCartridge;

		//The netID of the PDA, will be used later for the messenger
		private NetworkIdentity pdaId;

		// The name of the first person who put their ID into the PDA
		[NonSerialized] public string PdaRegisteredName;

		//Local storage of the PDA
		private ItemStorage storage;

		// a simple bool to see if the uplink is locked, remove Serialize field when done
		[SerializeField]
		private bool uplinkLocked;

		// The string that must be entered into the ringtone slot for the uplink
		[SyncVar]
		private string uplinkString;
		public string UplinkString => uplinkString;

		// A public readonly accessor for the TC
		public int TeleCrystals => teleCrystals;

		//Initial TC value, only assigned if antag
		[SerializeField]
		private int initalTeleCrystal;

		//The actual TC used, it's a syncvar so people cant to blackmagic fuckery to it
		[SyncVar]
		private int teleCrystals;

		[NonSerialized]
		public HasNetworkTabItem TabOnGameObject;

		//What antag the PDA should look for when running antagcheck
		[SerializeField]
		private Antagonist antagSet;

		private UplinkPasswordList passlist;

		[SerializeField] private ActionData actionData;

		public ActionData ActionData => actionData;


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
			else if (hand != null && (hand.GetComponent<PDACartridge>() != null))
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
			storage = gameObject.GetComponent<ItemStorage>();
			TabOnGameObject = gameObject.GetComponent<HasNetworkTabItem>();
			pdaId = gameObject.GetComponent<NetworkIdentity>();
			var slot = storage.GetIndexedItemSlot(0);
			if (slot.IsEmpty != true) IdCard = slot.Item.GetComponent<IDCard>();
			slot.OnSlotContentsChangeServer.AddListener(SlotChange);
			passlist = UplinkPasswordList.Instance;
			//messengerSystem = GameObject.Find("MessengerManager").GetComponent<MessengerManager>();
			//AddSelf();
		}
		// checks to see if the character is the antag set in editor, if so enables uplink and gives TC
		[Server]
		public void AntagCheck(GameObject player)
		{
			//TODO This is a hacky fix someone please figure out why the GUI start method runs twice
			uplinkString = null;
			SpawnedAntag antag = player.GetComponent<PlayerScript>().mind.GetAntag();
			if (antag == null) return;
			string antagName = antag.Antagonist.AntagName;
			if (antagName != antagSet.AntagName || !isServer) return;
			teleCrystals = initalTeleCrystal;
			UplinkGenerate();
			uplinkLocked = false;
			PlayerCodeRemind(player);
		}
		[Server]
		private void UplinkGenerate()
		{
			for (int i = 0; i < 3; i++)
			{
				string word = passlist.WordList[Random.Range(0,passlist.WordList.Count)];
				uplinkString += word;
			}

			string nums = Random.Range(111, 999).ToString();
			uplinkString += nums;
		}

		private void PlayerCodeRemind(GameObject playerref)
		{
			Chat.AddExamineMsgFromServer(playerref, $"You suddenly remember reading an ominous message saying UplinkString: {uplinkString}");
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
			return notificationString == uplinkString && isServer && uplinkLocked != true;
		}
		/// <summary>
		/// Spawns the item requested by the uplink
		/// </summary>
		[Server]
		public void SpawnUplinkItem(GameObject objectRequested, int cost)
		{
			// Grabs the player's playerscript by asking HasNetworkTabItem who was the last person to interact
			if (cost <= teleCrystals)
			{
				var player = TabOnGameObject.LastInteractedPlayer().GetComponent<PlayerScript>().ItemStorage;
				var result = Spawn.ServerPrefab(objectRequested);
				var item = result.GameObject;
				teleCrystals -=cost;
				if (player.GetNamedItemSlot(NamedSlot.rightHand) == null)
				{
					Inventory.ServerAdd(item, player.GetNamedItemSlot(NamedSlot.rightHand),
						ReplacementStrategy.DropOther);
				}
				Inventory.ServerAdd(item, player.GetNamedItemSlot(NamedSlot.leftHand),
					ReplacementStrategy.DropOther);
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
			IdCard = null;
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

		//TODO Get someone else to do the networking for the messenger
		//The methods below handle any PDA messages that get sent to this PDA, not being used please come back later
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
		// All these methods handle ID card access, should only be ran server side because we cant trust client
		//Note I I dont 100% understand all this but it works on the old code so im guessing its all fine
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
}
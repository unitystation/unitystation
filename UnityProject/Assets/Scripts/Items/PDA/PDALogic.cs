using System;
using System.Collections;
using UnityEngine;
using Mirror;
using Antagonists;
using Random = UnityEngine.Random;
using UI.Action;

namespace Items.PDA
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(ItemLightControl))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(PDANotesNetworkHandler))]
	public class PDALogic : NetworkBehaviour, ICheckedInteractable<HandApply>, ICheckedInteractable<InventoryApply>
	{
		private const bool DEBUG_UPLINK = false;
		private const string DEBUG_UPLINK_CODE = "Whiskey Tango Foxtrot-1337";
		private const int TIME_BEFORE_UPLINK_ACTIVATION = 10;

		#region Inspector
		[Tooltip("The cartridge prefab to be spawned for this PDA")]
		[SerializeField]
		private GameObject cartridgePrefab = default;

		[Tooltip("The default ringtone to play")]
		[SerializeField]
		private string defaultRingtone = "TwoBeep";

		[Tooltip("Reset registered name on FactoryReset?")]
		[SerializeField]
		private bool willResetName = false;

		#endregion Inspector

		// GameObject attached components
		private NetworkIdentity pdaID;
		private Pickupable pickupable;
		private ItemStorage storage;
		private ItemLightControl flashlight;
		private ItemActionButton actionButton;

		/// <summary> The IDCard that is currently inserted into the PDA </summary>
		public IDCard IDCard { get; private set; }
		/// <summary> The name of the currently registered player (since the last PDA reset) </summary>
		public string RegisteredPlayerName { get; private set; }
		public string Ringtone { get; private set; }
		/// <summary> The string that must be entered into the ringtone slot for the uplink </summary>
		public string UplinkUnlockCode { get; private set; }
		public bool IsUplinkCapable { get; private set; } = false;
		public bool IsUplinkLocked { get; private set; } = true;
		/// <summary> The count of how many telecrystals this PDA has </summary>
		public int UplinkTC { get; private set; }

		public bool FlashlightOn => flashlight.IsOn;

		public Action registeredPlayerUpdated;
		public Action idSlotUpdated;
		public Action cartridgeSlotUpdated;

		private ItemSlot IDSlot = default;
		private ItemSlot CartridgeSlot = default;

		#region Messenger stuff (unused)

		// for messenger system
		//MessengerSyncDictionary pdas;

		//The actual list of access allowed set via the server and synced to all clients
		private readonly SyncListInt accessSyncList = new SyncListInt();

		//private MessengerManager messengerSystem;

		#endregion Messenger stuff (unused)

		#region Lifecycle

		private void Awake()
		{
			pdaID = GetComponent<NetworkIdentity>();
			pickupable = GetComponent<Pickupable>();
			storage = GetComponent<ItemStorage>();
			flashlight = GetComponent<ItemLightControl>();
			actionButton = GetComponent<ItemActionButton>();
		}

		private void OnEnable()
		{
			actionButton.ServerActionClicked += ToggleFlashlight;
		}

		private void OnDisable()
		{
			actionButton.ServerActionClicked -= ToggleFlashlight;
		}

		private void Start()
		{
			SetRingtone(defaultRingtone);

			IDSlot = storage.GetIndexedItemSlot(0);
			CartridgeSlot = storage.GetIndexedItemSlot(1);

			IDSlot.OnSlotContentsChangeServer.AddListener(OnIDSlotChanged);
			//messengerSystem = GameObject.Find("MessengerManager").GetComponent<MessengerManager>();
			//AddSelf();
		}

		public override void OnStartServer()
		{
			// TODO Instead, consider listening for client's OnPlayerSpawned and then request server to run stuff.
			StartCoroutine(DelayInitialisation());
		}

		private IEnumerator DelayInitialisation()
		{
			yield return WaitFor.Seconds(TIME_BEFORE_UPLINK_ACTIVATION);
			OnStartServerDelayed();
		}

		private void OnStartServerDelayed()
		{
			if (cartridgePrefab != null)
			{
				var cartridge = Spawn.ServerPrefab(cartridgePrefab).GameObject;
				Inventory.ServerAdd(cartridge, CartridgeSlot);
			}

			var owner = GetPlayerByParentInventory();
			if (owner == null) return;

			RegisteredPlayerName = owner.ExpensiveName();
			TryInstallUplink(owner);

			if (DEBUG_UPLINK)
			{
#pragma warning disable CS0162 // Unreachable code detected
				UplinkTC = 20;
				UplinkUnlockCode = DEBUG_UPLINK_CODE;
				IsUplinkCapable = true;
				InformUplinkCode(owner);
#pragma warning restore CS0162 // Unreachable code detected
			}
		}

		#endregion Lifecycle

		public void ToggleFlashlight()
		{
			flashlight.Toggle(!flashlight.IsOn);
		}

		/// <summary>
		/// Resets PDA and tells the MessengerManager to set PDA to unknown
		/// </summary>
		public void ResetPDA()
		{
			if (RegisteredPlayerName == default ||
					IDSlot.IsOccupied && IDCard.RegisteredName == RegisteredPlayerName)
			{
				if (willResetName)
				{
					ReplaceName(pdaID, "UnknownPDA");
					RegisteredPlayerName = default;
				}

				LockUplink();
				SetRingtone(defaultRingtone);
				PlayRingtone();

				registeredPlayerUpdated?.Invoke();
			}

			else
			{
				PlayDenyTone();
			}
		}

		private GameObject GetPlayerByParentInventory()
		{
			if (pickupable.ItemSlot == null) return default;

			return pickupable.ItemSlot.RootPlayer().gameObject;
		}

		#region Sounds

		public void SetRingtone(string newRingtone)
		{
			Ringtone = newRingtone;
		}

		public void PlayRingtone()
		{
			PlaySound(Ringtone);
		}

		public void PlayDenyTone()
		{
			PlaySound("BuzzDeny");
		}

		public void PlaySound(string soundName)
		{
			var sourceObject = GetPlayerByParentInventory();
			if (sourceObject == null)
			{
				sourceObject = gameObject;
			}

			// JESTE_R
			//SoundManager.PlayNetworkedAtPos(soundName, sourceObject.AssumedWorldPosServer(), sourceObj: sourceObject);
		}

		public void PlaySoundPrivate(string soundName)
		{
			var player = GetPlayerByParentInventory();
			if (player == null) return;

			// JESTE_R
			//SoundManager.PlayNetworkedForPlayerAtPos(player, player.AssumedWorldPosServer(), soundName, sourceObj: player);
		}

		#endregion Sounds

		#region Interaction
		// HandApply for clicking on the PDA while it is on the ground.
		// InventoryApply for clicking on the PDA while in inventory.

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.IsAltClick && IDSlot.IsOccupied) return true;

			return WillInsert(interaction.UsedObject, side);
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.IsAltClick && IDSlot.IsOccupied)
			{
				EjectIDCard();
				return;
			}

			ServerInsertItem(interaction.UsedObject, interaction.HandSlot);
		}

		public bool WillInteract(InventoryApply interaction, NetworkSide side)
		{
			if (!DefaultWillInteract.Default(interaction, side)) return false;
			if (interaction.IsAltClick && IDSlot.IsOccupied) return true;

			return WillInsert(interaction.UsedObject, side);
		}

		public void ServerPerformInteraction(InventoryApply interaction)
		{
			if (interaction.IsAltClick && IDSlot.IsOccupied)
			{
				EjectIDCard();
				return;
			}

			ServerInsertItem(interaction.UsedObject, interaction.FromSlot);
		}

		private bool WillInsert(GameObject item, NetworkSide side)
		{
			if (item == null) return false;

			if (item.TryGetComponent(out IDCard card) && Validations.CanFit(IDSlot, item, side, true))
			{
				return true;
			}

			if (item.TryGetComponent(out PDACartridge cartridge) && Validations.CanFit(CartridgeSlot, item, side, true))
			{
				return true;
			}

			return false;
		}

		private void ServerInsertItem(GameObject item, ItemSlot fromSlot)
		{
			if (item.TryGetComponent(out IDCard card))
			{
				if (IDSlot.IsOccupied)
				{
					EjectIDCard();
				}

				Inventory.ServerTransfer(fromSlot, IDSlot);
				IDCard = card;
			}

			else if (item.TryGetComponent(out PDACartridge cartridge))
			{
				if (CartridgeSlot.IsOccupied)
				{
					EjectCartridge();
				}

				Inventory.ServerTransfer(fromSlot, CartridgeSlot);
			}
		}

		#endregion Interaction

		#region Uplink-Init

		private void TryInstallUplink(GameObject owner)
		{
			var antagType = TryGetAntagType(owner);
			if (antagType == null) return;

			UplinkTC = GetTCFromAntag(antagType);
			if (UplinkTC <= 0) return;

			UplinkUnlockCode = GenerateUplinkUnlockCode();
			IsUplinkCapable = true;

			InformUplinkCode(owner);
		}

		private Antagonist TryGetAntagType(GameObject player)
		{
			var playerMind = player.GetComponent<PlayerScript>().mind;
			if (playerMind.IsAntag)
			{
				return playerMind.GetAntag().Antagonist;
			}

			return default;
		}

		private int GetTCFromAntag(Antagonist antag)
		{
			if (antag is Antagonists.Traitor traitor)
			{
				return traitor.initialTC;
			}
			else if (antag is NuclearOperative operative)
			{
				return operative.initialTC;
			}

			return default;
		}

		private string GenerateUplinkUnlockCode()
		{
			var code = "";
			var codeList = UplinkPasswordList.Instance.WordList;
			for (int i = 0; i < 3; i++)
			{
				string word = codeList[Random.Range(0, codeList.Count)];
				code += word;
			}

			string nums = Random.Range(111, 999).ToString();
			return code += nums;
		}

		private void InformUplinkCode(GameObject player)
		{
			var uplinkMessage =
					$"{(DEBUG_UPLINK ? "<b>UPLINK DEBUGGING ENABLED: </b>" : "")}" +
					$"The Syndicate has cunningly disguised a <i>Syndicate Uplink</i> as your <i>{gameObject.ExpensiveName()}</i>." +
					$"Simply enter the code <b>{UplinkUnlockCode}</b> into the ringtone select to unlock its hidden features.";

			PlaySoundPrivate(Ringtone);
			Chat.AddExamineMsgFromServer(player, uplinkMessage);
		}

		#endregion Uplink-Init

		#region Uplink

		[Server]
		public void LockUplink()
		{
			IsUplinkLocked = true;
		}

		[Server]
		public void UnlockUplink(string attemptedCode)
		{
			if (attemptedCode == UplinkUnlockCode)
			{
				IsUplinkLocked = false;
			}
		}

		/// <summary>
		/// Spawns the item requested by the uplink if there are enough TC.
		/// </summary>
		[Server]
		public void SpawnUplinkItem(GameObject objectRequested, int cost)
		{
			if (!IsUplinkCapable || IsUplinkLocked) return;

			if (cost > UplinkTC) return;

			var result = Spawn.ServerPrefab(objectRequested);
			if (result.Successful)
			{
				UplinkTC -= cost;
				var item = result.GameObject;
				Inventory.ServerAdd(item, GetBestSlot(item));
			}
		}

		#endregion Uplink

		#region Inventory

		private void OnIDSlotChanged()
		{
			IDCard = null;
			if (IDSlot.IsOccupied && isServer)
			{
				IDCard = IDSlot.Item.GetComponent<IDCard>();

				if (RegisteredPlayerName == default)
				{
					RegisteredPlayerName = IDCard.RegisteredName;
					ReplaceName(pdaID, RegisteredPlayerName);
				}
			}

			idSlotUpdated?.Invoke();
		}

		public void EjectIDCard()
		{
			EjectSlotContents(IDSlot);
		}

		public void EjectCartridge()
		{
			EjectSlotContents(CartridgeSlot);
		}

		private void EjectSlotContents(ItemSlot slot)
		{
			var bestSlot = GetBestSlot(slot.ItemObject);
			if (!Inventory.ServerTransfer(slot, bestSlot))
			{
				Inventory.ServerDrop(IDSlot);
			}
		}

		private ItemSlot GetBestSlot(GameObject item)
		{
			var player = GetPlayerByParentInventory();
			if (player == null)
			{
				return default;
			}

			var playerStorage = player.GetComponent<PlayerScript>().ItemStorage;
			return playerStorage.GetBestHandOrSlotFor(item);
		}

		#endregion Inventory

		#region Messaging (unused)

		//Just to make sure this only runs on client as Cmd only works Client ---> server
		[Client]
		private void ReplaceName(NetworkIdentity pdaId, string newName)
		{
			//messengerSystem.ReplaceName(pdaID, newName);
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

		#endregion Messaging

		#region IDAccess
		// All these methods handle ID card access, should only be ran server side because we cant trust client
		//Note I I dont 100% understand all this but it works on the old code so im guessing its all fine

		[Server]
		public bool HasAccess(Access access)
		{
			return accessSyncList.Contains((int)access);
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
			accessSyncList.Remove((int)access);
		}

		// Adds the indicated access to this IDCard
		[Server]
		public void ServerAddAccess(Access access)
		{
			if (HasAccess(access)) return;
			accessSyncList.Add((int)access);
		}

		#endregion IDAccess
	}
}

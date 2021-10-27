using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Clearance;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using Random = UnityEngine.Random;
using UI.Action;
using AddressableReferences;
using UI.Items.PDA;

namespace Items.PDA
{
	[RequireComponent(typeof(ItemStorage))]
	[RequireComponent(typeof(ItemLightControl))]
	[RequireComponent(typeof(ItemAttributesV2))]
	[RequireComponent(typeof(PDANotesNetworkHandler))]
	public class PDALogic : NetworkBehaviour,
		ICheckedInteractable<HandApply>,
		ICheckedInteractable<InventoryApply>,
		IServerInventoryMove,
		IClearanceProvider
	{
		// TODO: consider moving uplink code into its own class (perhaps compatible with pen, headset uplinks)

		#region Inspector
		[Tooltip("The cartridge prefab to be spawned for this PDA")]
		[SerializeField, BoxGroup("Settings")]
		private GameObject cartridgePrefab = default;

		[Tooltip("The default ringtone to play")]
		[SerializeField, BoxGroup("Settings")]
		private AddressableAudioSource defaultRingtone;

		[Tooltip("A list of all available ringtones")]
		[SerializeField, BoxGroup("Settings")]
		private List<AddressableAudioSource> ringtones;

		[Tooltip("The denial sound")]
		[SerializeField, BoxGroup("Settings")]
		private AddressableAudioSource denialSound;

		[Tooltip("Reset registered name on FactoryReset?")]
		[SerializeField, BoxGroup("Settings")]
		private bool willResetName = false;

		[Tooltip("Tint of the main background in the GUI")]
		[BoxGroup("GUI")]
		public Color UIBG;

		[Tooltip("Tint of the patterned overlay in the GUI")]
		[BoxGroup("GUI")]
		public Color UIOVER;

		[Tooltip("The overlay to be used in the GUI")]
		[BoxGroup("GUI")]
		public int OVERLAY;

		public GUI_PDA PDAGui;

		[Tooltip("How long the delay before the owner is informed of the uplink code " +
			"(intedned to reduce information overload - likely just received objectives)")]
		[SerializeField, BoxGroup("Uplink"), Range(0, 60)]
		private float informUplinkCodeDelay = 10;

		private bool isNukeOps = false;
		public bool IsNukeOps => isNukeOps;

		[SerializeField]
		private ItemTrait telecrystalTrait;

		[SerializeField, BoxGroup("Uplink")]
		private bool debugUplink = false;

		#endregion Inspector

		// GameObject attached components
		private Pickupable pickupable;
		private ItemStorage storage;
		private ItemLightControl flashlight;
		private ItemActionButton actionButton;

		/// <summary> The IDCard that is currently inserted into the PDA </summary>
		public IDCard IDCard { get; private set; }
		/// <summary> The name of the currently registered player (since the last PDA reset) </summary>
		public string RegisteredPlayerName { get; private set; }
		public AddressableAudioSource Ringtone { get; private set; }
		/// <summary> The string that must be entered into the ringtone slot for the uplink </summary>
		public string UplinkUnlockCode { get; private set; }
		public bool IsUplinkCapable { get; private set; } = false;
		public bool IsUplinkLocked { get; private set; } = true;
		/// <summary> The count of how many telecrystals this PDA has </summary>
		public int UplinkTC { get; set; }

		public bool FlashlightOn => flashlight.IsOn;

		public Action registeredPlayerUpdated;
		public Action idSlotUpdated;
		public Action cartridgeSlotUpdated;

		private ItemSlot IDSlot = default;
		private ItemSlot CartridgeSlot = default;

		//The actual list of access allowed set via the server and synced to all clients
		private readonly SyncList<int> accessSyncList = new SyncList<int>();

		#region Lifecycle

		private void Awake()
		{
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

			if (CustomNetworkManager.IsServer && cartridgePrefab != null)
			{
				var cartridge = Spawn.ServerPrefab(cartridgePrefab).GameObject;
				Inventory.ServerAdd(cartridge, CartridgeSlot);
			}
		}

		#endregion Lifecycle

		public void OnInventoryMoveServer(InventoryMove info)
		{
			// This is also triggered when the PDA spawns as part of the player's inventory populator
			// and added to their inventory, when they spawn.

			if (RegisteredPlayerName != default) return; // PDA already registered to someone
			if (info.ToRootPlayer == null) return; // PDA was not added to player

			ConnectedPlayer pickedUpBy = info.ToRootPlayer.gameObject.Player();
			RegisterTo(pickedUpBy);

			if (debugUplink)
			{
				InstallUplink(pickedUpBy, 80, true);
			}
		}

		private void RegisterTo(ConnectedPlayer player)
		{
			RegisteredPlayerName = player.Script.playerName;
			gameObject.name = $"{player.Script.playerName}'s PDA ({player.Script.mind.occupation.DisplayName})";
			gameObject.Item().ServerSetArticleName(gameObject.name);
		}

		private void RegisterTo(string playerName)
		{
			RegisteredPlayerName = playerName;
			gameObject.name = playerName == default ? gameObject.Item().InitialName : $"{playerName}'s PDA";
			gameObject.Item().ServerSetArticleName(gameObject.name);
		}

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
					RegisterTo(default(string));
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

		private ConnectedPlayer GetPlayerByParentInventory()
		{
			if (pickupable.ItemSlot == null) return default;

			return pickupable.ItemSlot.RootPlayer().gameObject.Player();
		}

		#region Sounds

		public void SetRingtone(AddressableAudioSource newRingtone)
		{
			Ringtone = newRingtone;
		}

		public void SetRingtone(string newRingtone)
		{
			AddressableAudioSource toSend = ringtones.Find(x => x.AssetAddress.Contains("/" + newRingtone + ".prefab"));

			if(toSend != default(AddressableAudioSource))
				SetRingtone(toSend);
		}

		public void PlayRingtone()
		{
			PlaySound(Ringtone);
		}

		public void PlayDenyTone()
		{
			PlaySound(denialSound);
		}

		public void PlaySound(AddressableAudioSource soundName)
		{
			GameObject sourceObject = gameObject;

			ConnectedPlayer player = GetPlayerByParentInventory();
			if (player != null)
			{
				sourceObject = player.GameObject;
			}

			SoundManager.PlayNetworkedAtPos(soundName, sourceObject.AssumedWorldPosServer(), sourceObj: sourceObject);
		}

		public void PlaySoundPrivate(AddressableAudioSource soundName)
		{
			var player = GetPlayerByParentInventory();
			if (player == null) return;

			_ = SoundManager.PlayNetworkedForPlayerAtPos(player.GameObject, player.Script.WorldPos, soundName, sourceObj: player.GameObject);
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

			ServerInsertItem(interaction.UsedObject, interaction.HandSlot, interaction.Performer);
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

			ServerInsertItem(interaction.UsedObject, interaction.FromSlot , interaction.Performer);
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

			if (Validations.HasItemTrait(item, telecrystalTrait))
			{
				return true;
			}

			return false;
		}

		private void ServerInsertItem(GameObject item, ItemSlot fromSlot, GameObject player)
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
			else if (Validations.HasItemTrait(item, telecrystalTrait))
			{
				if (IsUplinkLocked == false)
				{
					var quantity = item.GetComponent<Stackable>().Amount;
					UplinkTC +=  quantity;
					_ = Despawn.ServerSingle(item);

					var uplinkMessage =
						$"You press the Telecrystal{(quantity == 1 ? "" : "s")} into the screen of your {this.gameObject.ExpensiveName()}\n" +
						$"After a moment it disappears, your Telecrystal counter ticks up a second later";

					Chat.AddExamineMsgFromServer(player, uplinkMessage);
					UpdateTCCountGui();
				}
			}
		}

		public void UpdateTCCountGui()
		{
			if (PDAGui)
			{
				PDAGui.uplinkPage.UpdateTCCounter();
			}
		}

		#endregion Interaction

		#region Uplink-Init

		/// <summary>
		/// Installs uplink capability into this PDA with the given telecrystal count and informs the given player the code.
		/// </summary>
		/// <param name="informPlayer">The player that will be informed the code to the PDA uplink</param>
		/// <param name="tcCount">The amount of telecrystals to add to the uplink.</param>
		/// <param name="isNukie">Determines if the uplink can purchase nukeop exclusive items</param>
		public void InstallUplink(ConnectedPlayer informPlayer, int tcCount, bool isNukie)
		{
			UplinkTC = tcCount; // Add; if uplink installed again (e.g. via admin tools (player request more TC)).
			UplinkUnlockCode = GenerateUplinkUnlockCode();
			IsUplinkCapable = true;
			isNukeOps = isNukie;

			StartCoroutine(DelayInformUplinkCode(informPlayer));
		}

		private string GenerateUplinkUnlockCode()
		{
			var codeList = UplinkPasswordList.Instance.WordList;
			
			string code = codeList[Random.Range(0, codeList.Count)];

			string nums = Random.Range(111, 999).ToString();
			return code + nums;
		}

		private IEnumerator DelayInformUplinkCode(ConnectedPlayer forPlayer)
		{
			// We delay the uplink code inform to reduce information overload (player was likely just given objectives)
			yield return WaitFor.Seconds(informUplinkCodeDelay);
			InformUplinkCode(forPlayer);
		}

		private void InformUplinkCode(ConnectedPlayer player)
		{
			var uplinkMessage =
					$"{(debugUplink ? "<b>UPLINK DEBUGGING ENABLED: </b>" : "")}" +
					$"</i>The Syndicate has cunningly disguised a <i>Syndicate Uplink</i> as your <i>{gameObject.ExpensiveName()}</i>. " +
					$"Simply enter the code <b>{UplinkUnlockCode}</b> into the ringtone select to unlock its hidden features.<i>";

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

			var result = Spawn.ServerPrefab(objectRequested,GetComponent<Pickupable>().ItemSlot.Player.WorldPosition, PrePickRandom: true);
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
					RegisterTo(IDCard.RegisteredName);
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
				Inventory.ServerDrop(slot);
			}
		}

		private ItemSlot GetBestSlot(GameObject item)
		{
			var player = GetPlayerByParentInventory();
			if (player == null)
			{
				return default;
			}

			var playerStorage = player.Script.DynamicItemStorage;
			return playerStorage.GetBestHandOrSlotFor(item);
		}

		#endregion Inventory

		#region IDAccess
		// All these methods handle ID card access, should only be ran server side because we cant trust client

		[Server]
		public bool HasAccess(Access access)
		{
			return accessSyncList.Contains((int)access);
		}

		[Server]
		public SyncList<int> AccessList()
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

		// All the methods above will be obsolete as soon as we migrate
		public IEnumerable<Clearance> GetClearance()
		{
			var idClearance = IDCard.OrNull()?.GetComponent<IClearanceProvider>();

			return idClearance?.GetClearance();
		}

		#endregion IDAccess
	}
}

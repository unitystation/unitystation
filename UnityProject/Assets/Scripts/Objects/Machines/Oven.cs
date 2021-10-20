using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using AddressableReferences;
using Audio.Containers;
using Messages.Server.SoundMessages;
using Systems.Electricity;
using Items;
using Items.Food;
using Machines;
using Objects.Machines;


namespace Objects.Kitchen
{
	/// <summary>
	/// A machine into which players can insert items for cooking. If the item has the Cookable component,
	/// the item will be cooked once enough time has lapsed as determined in that component.
	/// </summary>
	public class Oven : NetworkBehaviour, IAPCPowerable, IRefreshParts
	{

		private enum SpriteStateOven
		{
			Idle = 0,
			Running = 1
		}
		private enum SpriteStateDoor
		{
			Open = 0,
			Closed = 1
		}

		[SerializeField]
		private AddressableAudioSource doorOpenSfx = null; // Sfx the oven door should make when opening.
		[SerializeField]
		private AddressableAudioSource doorCloseSfx = null; // Sfx the oven door should make when closing.

		[SerializeField] private AddressableAudioSource startSfx = null;

		[SerializeField]
		[Tooltip("The looped audio source to play while the oven is running.")]
		private AddressableAudioSource RunningAudio = default;

		private string runLoopGUID = "";

		[SerializeField]
		[Tooltip("Child GameObject that is responsible for the screen glow.")]
		private GameObject ScreenGlow = default;

		[SerializeField]
		[Tooltip("Child GameObject that is responsible for the glow from the interior oven when the door is open.")]
		private GameObject OvenGlow = default;

		[SerializeField]
		[Tooltip("Storage structure to use for tier 1-4 matter bins.")]
		private ItemStorageStructure[] TierStorage = new ItemStorageStructure[4];

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the oven's on light.")]
		private int circuitWattage = 5;

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the oven oven's bulb.")]
		private int ovenBulbWattage = 25;

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the oven oven's heating coil (T1 laser stock part).")]
		private int magnetronWattage = 850;

		private RegisterTile registerTile;
		[SerializeField]
		[Tooltip("Sprite responsible for the oven itself.")]
		private SpriteHandler spriteHandlerOven = default;
		[SerializeField]
		[Tooltip("Sprite responsible for the oven's door.")]
		private SpriteHandler spriteHandlerDoor = default;
		private ItemStorage storage;
		private APCPoweredDevice poweredDevice;
		private readonly Dictionary<ItemSlot, Cookable> storedCookables = new Dictionary<ItemSlot, Cookable>();

		[SyncVar(hook = nameof(OnSyncPlayAudioLoop))]
		private bool playAudioLoop;

		[SyncVar(hook = nameof(OnSyncScreenGlow))]
		private bool screenGlowEnabled = true;
		[SyncVar(hook = nameof(OnSyncOvenGlow))]
		private bool ovenGlowEnabled = false;

		public bool IsOperating => CurrentState is OvenRunning;
		
		public IEnumerable<ItemSlot> Slots => storage.GetItemSlots();
		public bool HasContents => Slots.Any(Slot => Slot.IsOccupied);
		public int StorageSize => storage.StorageSize();

		public Vector3Int WorldPosition => registerTile.WorldPosition;

		// Stock Part Tier.
		private int laserTier = 1;
		private float voltageModifier = 1;

		/// <summary>
		/// The micro-laser's tier affects the speed in which the oven counts down and the speed
		/// in which food is cooked. For each tier above one, cook time is decreased by a factor of 0.5
		/// </summary>
		private float LaserModifier => Mathf.Pow(2, laserTier - 1);

		private float Effectiveness => LaserModifier * voltageModifier;

		public OvenState CurrentState { get; private set; }

		#region Lifecycle

		// Interfaces can call this script before Awake() is called.
		private void EnsureInit()
		{
			if (CurrentState != null) return;

			registerTile = GetComponent<RegisterTile>();
			storage = GetComponent<ItemStorage>();
			poweredDevice = GetComponent<APCPoweredDevice>();

			SetState(new OvenUnpowered(this));
		}

		private void Start()
		{
			EnsureInit();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion

		/// <summary>
		/// Reduce the oven's timer, add that time to food cooking.
		/// </summary>
		private void UpdateMe()
		{
			if (IsOperating == false) return;

			float cookTime = Time.deltaTime * Effectiveness;

			CheckCooked(cookTime);
		}

		private void SetState(OvenState newState)
		{
			CurrentState = newState;
		}

		#region Requests

		/// <summary>
		/// Starts or stops the oven, depending on the oven's current state.
		/// </summary>
		public void RequestToggleActive()
		{
			CurrentState.ToggleActive();
		}

		/// <summary>
		/// Opens or closes the oven's door or adds items to the oven, depending on the oven's current state.
		/// </summary>
		/// <param name="fromSlot">The slot with which to check if an item is held, for inserting an item when closing.</param>
		public void RequestDoorInteraction(PositionalHandApply interaction = null)
		{
			CurrentState.DoorInteraction(interaction);
		}

		#endregion

		private bool TryTransferToOven(PositionalHandApply interaction)
		{
			// Close the oven if nothing is held.
			if (interaction == null || interaction.UsedObject == null)
			{
				SoundManager.PlayNetworkedAtPos(doorCloseSfx, WorldPosition, sourceObj: gameObject);
				return false;
			}

			// Add held item to the oven.
			ItemSlot storageSlot = storage.GetNextFreeIndexedSlot();
			bool added = false;
			if (interaction.HandSlot.ItemObject.TryGetComponent(out Stackable stack))
			{
				if(stack.Amount == 1)
				{
					added = Inventory.ServerTransfer(interaction.HandSlot, storageSlot);
				}
				else
				{
					var item = stack.ServerRemoveOne(); 
					added = Inventory.ServerAdd(item, storageSlot);
					if(!added)
					{
						stack.ServerIncrease(1); // adds back the lost amount to prevent ovens from eating stackable items
					}
				}
			}
			else
			{
				added = Inventory.ServerTransfer(interaction.HandSlot, storageSlot);
			}
			if (storageSlot == null || added == false)
			{
				Chat.AddActionMsgToChat(interaction, "The oven's matter bins are full!", string.Empty);
				return true;
			}

			if (storageSlot.ItemObject.TryGetComponent(out Cookable cookable) && cookable.CookableBy.HasFlag(CookSource.Oven))
			{
				storedCookables.Add(storageSlot, cookable);
			}

			Chat.AddActionMsgToChat(
					interaction,
					$"You add the {interaction.HandObject.ExpensiveName()} to the oven.",
					$"{interaction.PerformerPlayerScript.visibleName} adds the {interaction.HandObject.ExpensiveName()} to the oven.");

			return true;
		}

		private void OpenOvenAndEjectContents()
		{
			// Looks nicer if we drop the item in the middle of the sprite's representation of the oven's interior.
			Vector2 spritePosWorld = spriteHandlerOven.transform.position;
			Vector2 ovenInteriorCenterAbs = spritePosWorld + new Vector2(-0.075f, -0.075f);
			Vector2 ovenInteriorCenterRel = ovenInteriorCenterAbs - WorldPosition.To2Int();

			SoundManager.PlayNetworkedAtPos(doorOpenSfx, ovenInteriorCenterAbs, sourceObj: gameObject);
			storage.ServerDropAll(ovenInteriorCenterRel);

			storedCookables.Clear();
		}

		private void StartOven(bool silent)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			if(!silent)
				SoundManager.PlayNetworkedAtPos(startSfx, WorldPosition, sourceObj: gameObject);
			playAudioLoop = true;
		}

		private void HaltOven(bool silent)
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			if(!silent)
				SoundManager.PlayNetworkedAtPos(startSfx, WorldPosition, sourceObj: gameObject);
			playAudioLoop = false;
		}

		private void CheckCooked(float cookTime)
		{
			for (int i = storedCookables.Count - 1; i >= 0; i--)
			{
				var slot = storedCookables.Keys.ElementAt(i);
				var cookable = storedCookables[slot];

				// True if the item's total cooking time exceeds the item's minimum cooking time.
				if (cookable.AddCookingTime(cookTime))
				{
					// Swap item for its cooked version, if applicable.
					if (cookable.CookedProduct == null) return;

					_ = Despawn.ServerSingle(cookable.gameObject);
					GameObject cookedItem = Spawn.ServerPrefab(cookable.CookedProduct).GameObject;
					Inventory.ServerAdd(cookedItem, slot);
					if (cookedItem.TryGetComponent(out cookable))
					{
						storedCookables[slot] = cookable;
					}
					else
					{
						storedCookables.Remove(slot);
					}
				}
			}
		}

		private void OnSyncPlayAudioLoop(bool oldState, bool newState)
		{
			if (newState)
			{
				StartCoroutine(DelayOvenRunningSfx());
			}
			else
			{
				SoundManager.Stop(runLoopGUID);
			}
		}

		private void OnSyncScreenGlow(bool oldState, bool newState)
		{
			screenGlowEnabled = newState;
			ScreenGlow.SetActive(newState);
		}

		private void OnSyncOvenGlow(bool oldState, bool newState)
		{
			ovenGlowEnabled = newState;
			OvenGlow.SetActive(newState);
		}

		// We delay the running Sfx so the starting Sfx has time to play.
		private IEnumerator DelayOvenRunningSfx()
		{
			yield return WaitFor.Seconds(0.25f);

			// Check to make sure the state hasn't changed in the meantime.
			if (playAudioLoop)
			{
				runLoopGUID = Guid.NewGuid().ToString();
				SoundManager.PlayAtPositionAttached(RunningAudio, registerTile.WorldPosition, gameObject, runLoopGUID,
						audioSourceParameters: new AudioSourceParameters(pitch: voltageModifier));
			}
		}

		#region IRefreshParts

		public void RefreshParts(IDictionary<GameObject, int> partsInFrame)
		{
			EnsureInit();

			// Get the machine stock parts used in this instance and get the tier of each part.
			// Collection is unorganized so run through the whole list.
			foreach (GameObject part in partsInFrame.Keys)
			{
				ItemAttributesV2 partAttributes = part.GetComponent<ItemAttributesV2>();
				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MicroLaser))
				{
					laserTier = part.GetComponent<StockTier>().Tier;
				}

				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MatterBin))
				{
					int binTier = part.GetComponent<StockTier>().Tier;

					// Decide ItemStorageStructure based on tier. Currently: slot size == twice the bin tier.
					storage.AcceptNewStructure(TierStorage[binTier - 1]);
				}
			}
		}

		#endregion

		#region IAPCPowerable

		/// <summary>
		/// Part of interface IAPCPowerable. Sets the oven's state according to the given power state.
		/// </summary>
		/// <param name="state">The power state to set the oven's state with.</param>
		public void StateUpdate(PowerState state)
		{
			EnsureInit();
			CurrentState.PowerStateUpdate(state);
		}

		public void PowerNetworkUpdate(float voltage)
		{
			voltageModifier = voltage / 240;
		}

		private void SetWattage(float wattage)
		{
			poweredDevice.Wattusage = wattage * Effectiveness;
		}

		#endregion

		#region OvenStates

		public abstract class OvenState
		{
			public string StateMsgForExamine = "an unknown state";
			protected Oven oven;

			public abstract void ToggleActive();
			public abstract void DoorInteraction(PositionalHandApply interaction);
			public abstract void PowerStateUpdate(PowerState state);
		}

		private class OvenIdle : OvenState
		{
			public OvenIdle(Oven oven)
			{
				this.oven = oven;
				StateMsgForExamine = "idle";
				oven.spriteHandlerOven.ChangeSprite((int) SpriteStateOven.Idle);
				oven.spriteHandlerDoor.ChangeSprite((int) SpriteStateDoor.Closed);
				oven.OnSyncScreenGlow(oven.screenGlowEnabled, false);
				oven.OnSyncOvenGlow(oven.ovenGlowEnabled, false);
				oven.HaltOven(true);
				oven.SetWattage(oven.circuitWattage);
			}

			public override void ToggleActive()
			{
				oven.StartOven(false);
				oven.SetState(new OvenRunning(oven));
			}

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				oven.OpenOvenAndEjectContents();
				oven.SetState(new OvenOpen(oven));
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					oven.SetState(new OvenUnpowered(oven));
				}
			}
		}

		private class OvenOpen : OvenState
		{
			public OvenOpen(Oven oven)
			{
				this.oven = oven;
				StateMsgForExamine = "open";
				oven.spriteHandlerOven.ChangeSprite((int) SpriteStateOven.Idle);
				oven.spriteHandlerDoor.ChangeSprite((int) SpriteStateDoor.Open);
				oven.OnSyncScreenGlow(oven.screenGlowEnabled, false);
				oven.OnSyncOvenGlow(oven.ovenGlowEnabled, true);
				oven.HaltOven(true);
				oven.SetWattage(oven.circuitWattage + oven.ovenBulbWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				if (oven.TryTransferToOven(interaction) == false)
				{
					oven.SetState(new OvenIdle(oven));
				}
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					oven.SetState(new OvenUnpoweredOpen(oven));
				}
			}
		}

		private class OvenRunning : OvenState
		{
			public OvenRunning(Oven oven)
			{
				this.oven = oven;
				StateMsgForExamine = "running";
				oven.OnSyncScreenGlow(oven.screenGlowEnabled, true);
				oven.OnSyncOvenGlow(oven.ovenGlowEnabled, true);
				oven.spriteHandlerOven.ChangeSprite((int) SpriteStateOven.Running);
				oven.spriteHandlerDoor.ChangeSprite((int) SpriteStateDoor.Closed);
				oven.SetWattage(oven.circuitWattage + oven.ovenBulbWattage + oven.magnetronWattage);
			}

			public override void ToggleActive()
			{
				oven.HaltOven(false);
				oven.SetState(new OvenIdle(oven));
			}

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				oven.OpenOvenAndEjectContents();
				oven.SetState(new OvenOpen(oven));
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					oven.SetState(new OvenUnpowered(oven));
				}
			}
		}

		private class OvenUnpowered : OvenState
		{
			public OvenUnpowered(Oven oven)
			{
				this.oven = oven;
				StateMsgForExamine = "unpowered";
				oven.spriteHandlerOven.ChangeSprite((int) SpriteStateOven.Idle);
				oven.spriteHandlerDoor.ChangeSprite((int) SpriteStateDoor.Closed);
				oven.OnSyncScreenGlow(oven.screenGlowEnabled, false);
				oven.OnSyncOvenGlow(oven.ovenGlowEnabled, false);
				oven.HaltOven(true);
				oven.SetWattage(oven.circuitWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				oven.OpenOvenAndEjectContents();
				oven.SetState(new OvenUnpoweredOpen(oven));
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state != PowerState.Off)
				{
					oven.SetState(new OvenIdle(oven));
				}
			}
		}

		private class OvenUnpoweredOpen : OvenState
		{
			public OvenUnpoweredOpen(Oven oven)
			{
				this.oven = oven;
				StateMsgForExamine = "unpowered and open";
				oven.spriteHandlerOven.ChangeSprite((int) SpriteStateOven.Idle);
				oven.spriteHandlerDoor.ChangeSprite((int) SpriteStateDoor.Open);
				oven.OnSyncScreenGlow(oven.screenGlowEnabled, false);
				oven.OnSyncOvenGlow(oven.ovenGlowEnabled, false);
				oven.HaltOven(true);
				oven.SetWattage(oven.circuitWattage + oven.ovenBulbWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				if (oven.TryTransferToOven(interaction) == false)
				{
					oven.SetState(new OvenUnpowered(oven));
				}
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state != PowerState.Off)
				{
					oven.SetState(new OvenOpen(oven));
				}
			}
		}

		#endregion
	}
}

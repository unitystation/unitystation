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
	public class Microwave : NetworkBehaviour, IAPCPowerable, IRefreshParts
	{
		private const int MAX_TIMER_TIME = 60; // Seconds
		private const float DIRTY_CHANCE_PER_FINISH = 3; // Percent

		private enum SpriteState
		{
			Idle = 0,
			Open = 1,
			Running = 2,
			Unpowered = 3,
			UnpoweredOpen = 4,
			Broken = 5,
			BrokenOpen = 6,
		}

		[SerializeField]
		private AudioClipsArray doorSFX = null; // SFX the microwave door should make when opening/closing.

		[SerializeField]
		private AddressableAudioSource timerBeepSFX = null; // Beep to play when timer time is added/removed.

		[SerializeField]
		private AddressableAudioSource doneDingSFX = null; // Beep to play when timer time is added/removed.

		[SerializeField]
		[Range(0, MAX_TIMER_TIME)]
		[Tooltip("Default time the microwave should reset to when finished (in seconds). Should be set to a value that will cook most items.")]
		private float DefaultTimerTime = 10;

		[SerializeField] private AddressableAudioSource startSFX = null;

		[SerializeField]
		private AddressableAudioSource kaputSfx = default;

		[SerializeField]
		[Tooltip("The looped audio source to play while the microwave is running.")]
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
		[Tooltip("Wattage of the microwave's circuitry and display.")]
		private int circuitWattage = 5;

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the microwave oven's bulb.")]
		private int ovenBulbWattage = 25;

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the microwave oven's magnetron (T1 laser stock part).")]
		private int magnetronWattage = 850;

		/// <summary>
		/// How much time remains on the microwave's timer.
		/// </summary>
		[NonSerialized]
		public float MicrowaveTimer = 0;

		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private ItemStorage storage;
		private APCPoweredDevice poweredDevice;
		private readonly Dictionary<ItemSlot, Cookable> storedCookables = new Dictionary<ItemSlot, Cookable>();

		[SyncVar(hook = nameof(OnSyncPlayAudioLoop))]
		private bool playAudioLoop;

		[SyncVar(hook = nameof(OnSyncScreenGlow))]
		private bool screenGlowEnabled = true;
		[SyncVar(hook = nameof(OnSyncOvenGlow))]
		private bool ovenGlowEnabled = false;

		public bool IsOperating => CurrentState is MicrowaveRunning;
		
		public IEnumerable<ItemSlot> Slots => storage.GetItemSlots();
		public bool HasContents => Slots.Any(Slot => Slot.IsOccupied);
		public int StorageSize => storage.StorageSize();

		public Vector3Int WorldPosition => registerTile.WorldPosition;

		// Stock Part Tier.
		private int laserTier = 1;
		private float voltageModifier = 1;

		/// <summary>
		/// The micro-laser's tier affects the speed in which the microwave counts down and the speed
		/// in which food is cooked. For each tier above one, cook time is decreased by a factor of 0.5
		/// </summary>
		private float LaserModifier => Mathf.Pow(2, laserTier - 1);

		private float Effectiveness => LaserModifier * voltageModifier;

		private float contaminantModifier = 0;
		private float KaputChance => contaminantModifier + ((voltageModifier - 1) * 0.1f);

		public MicrowaveState CurrentState { get; private set; }

		#region Lifecycle

		// Interfaces can call this script before Awake() is called.
		private void EnsureInit()
		{
			if (CurrentState != null) return;

			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			storage = GetComponent<ItemStorage>();
			poweredDevice = GetComponent<APCPoweredDevice>();

			SetState(new MicrowaveUnpowered(this));
		}

		private void Start()
		{
			EnsureInit();

			MicrowaveTimer = DefaultTimerTime;
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion

		/// <summary>
		/// Reduce the microwave's timer, add that time to food cooking.
		/// </summary>
		private void UpdateMe()
		{
			if (IsOperating == false) return;

			float cookTime = Time.deltaTime * Effectiveness;
			MicrowaveTimer -= cookTime;

			if (MicrowaveTimer <= 0)
			{
				MicrowaveTimer = 0;
				MicrowaveTimerComplete();
			}

			CheckCooked(cookTime);
			CheckKaput();
		}

		private void SetState(MicrowaveState newState)
		{
			CurrentState = newState;
		}

		#region Requests

		/// <summary>
		/// Starts or stops the microwave, depending on the microwave's current state.
		/// </summary>
		public void RequestToggleActive()
		{
			CurrentState.ToggleActive();
		}

		/// <summary>
		/// Opens or closes the microwave's door or adds items to the microwave, depending on the microwave's current state.
		/// </summary>
		/// <param name="fromSlot">The slot with which to check if an item is held, for inserting an item when closing.</param>
		public void RequestDoorInteraction(PositionalHandApply interaction = null)
		{
			CurrentState.DoorInteraction(interaction);
		}

		/// <summary>
		/// Adds the given time to the microwave's timer. Can be negative.
		/// </summary>
		/// <param name="seconds">The amount of time, in seconds, to add to the timer. Can be negative.</param>
		public void RequestAddTime(int seconds)
		{
			CurrentState.AddTime(seconds);
		}

		#endregion

		private bool TryTransferToMicrowave(PositionalHandApply interaction)
		{
			// Close the microwave if nothing is held.
			if (interaction == null || interaction.UsedObject == null)
			{
				SoundManager.PlayNetworkedAtPos(doorSFX.GetRandomClip(), WorldPosition, sourceObj: gameObject);
				return false;
			}

			// Add held item to the microwave.
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
						stack.ServerIncrease(1); // adds back the lost amount to prevent microwaves from eating stackable items
					}
				}
			}
			else
			{
				added = Inventory.ServerTransfer(interaction.HandSlot, storageSlot);
			}
			if (storageSlot == null || added == false)
			{
				Chat.AddActionMsgToChat(interaction, "The microwave's matter bins are full!", string.Empty);
				return true;
			}

			if (storageSlot.ItemObject.TryGetComponent(out Cookable cookable) && cookable.CookableBy.HasFlag(CookSource.Microwave))
			{
				storedCookables.Add(storageSlot, cookable);
			}
			else
			{
				contaminantModifier += 0.02f;
			}

			Chat.AddActionMsgToChat(
					interaction,
					$"You add the {interaction.HandObject.ExpensiveName()} to the microwave.",
					$"{interaction.PerformerPlayerScript.visibleName} adds the {interaction.HandObject.ExpensiveName()} to the microwave.");

			return true;
		}

		private void OpenMicrowaveAndEjectContents()
		{
			// Looks nicer if we drop the item in the middle of the sprite's representation of the microwave's interior.
			Vector2 spritePosWorld = spriteHandler.transform.position;
			Vector2 microwaveInteriorCenterAbs = spritePosWorld + new Vector2(-0.075f, -0.075f);
			Vector2 microwaveInteriorCenterRel = microwaveInteriorCenterAbs - WorldPosition.To2Int();

			SoundManager.PlayNetworkedAtPos(doorSFX.GetRandomClip(), microwaveInteriorCenterAbs, sourceObj: gameObject);
			storage.ServerDropAll(microwaveInteriorCenterRel);

			storedCookables.Clear();
		}

		private void StartMicrowave()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			SoundManager.PlayNetworkedAtPos(startSFX, WorldPosition, sourceObj: gameObject);
			playAudioLoop = true;
		}

		private void HaltMicrowave()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			playAudioLoop = false;
		}

		private void AddTime(int seconds)
		{
			MicrowaveTimer = (MicrowaveTimer + seconds).Clamp(0, MAX_TIMER_TIME);

			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: seconds < 0 ? 0.8f : 1);
			SoundManager.PlayNetworkedAtPos(timerBeepSFX, WorldPosition, audioSourceParameters, sourceObj: gameObject);
		}

		private void MicrowaveTimerComplete()
		{
			HaltMicrowave();
			SoundManager.PlayNetworkedAtPos(doneDingSFX, WorldPosition, sourceObj: gameObject);

			// Chance to dirty microwave. Could probably tie this into what is cooked instead or additionally.
			if (DMMath.Prob(DIRTY_CHANCE_PER_FINISH))
			{
				DirtyMicrowave();
			}

			MicrowaveTimer = DefaultTimerTime;

			SetState(new MicrowaveIdle(this));
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

		private void CheckKaput()
		{
			if (DMMath.Prob(KaputChance))
			{
				SoundManager.PlayNetworkedAtPos(kaputSfx, WorldPosition, sourceObj: gameObject);
				SetState(new MicrowaveBroken(this));
			}
		}

		public void DirtyMicrowave()
		{
			spriteHandler.ChangeSpriteVariant(1);
		}

		public void CleanMicrowave()
		{
			spriteHandler.ChangeSpriteVariant(0);
		}

		private void OnSyncPlayAudioLoop(bool oldState, bool newState)
		{
			if (newState)
			{
				StartCoroutine(DelayMicrowaveRunningSFX());
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

		// We delay the running SFX so the starting SFX has time to play.
		private IEnumerator DelayMicrowaveRunningSFX()
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

					// Decide ItemStorageStructure based on tier. Currently: slot size == matter bin tier.
					storage.AcceptNewStructure(TierStorage[binTier - 1]);
				}
			}
		}

		#endregion

		#region IAPCPowerable

		/// <summary>
		/// Part of interface IAPCPowerable. Sets the microwave's state according to the given power state.
		/// </summary>
		/// <param name="state">The power state to set the microwave's state with.</param>
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

		#region MicrowaveStates

		public abstract class MicrowaveState
		{
			public string StateMsgForExamine = "an unknown state";
			protected Microwave microwave;

			public abstract void ToggleActive();
			public abstract void DoorInteraction(PositionalHandApply interaction);
			public abstract void AddTime(int seconds);
			public abstract void PowerStateUpdate(PowerState state);
		}

		private class MicrowaveIdle : MicrowaveState
		{
			public MicrowaveIdle(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "idle";
				microwave.spriteHandler.ChangeSprite((int) SpriteState.Idle);
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, true);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, false);
				microwave.HaltMicrowave();
				microwave.SetWattage(microwave.circuitWattage);
			}

			public override void ToggleActive()
			{
				microwave.StartMicrowave();
				microwave.SetState(new MicrowaveRunning(microwave));
			}

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				microwave.OpenMicrowaveAndEjectContents();
				microwave.SetState(new MicrowaveOpen(microwave));
			}

			public override void AddTime(int seconds)
			{
				microwave.AddTime(seconds);
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					microwave.SetState(new MicrowaveUnpowered(microwave));
				}
			}
		}

		private class MicrowaveOpen : MicrowaveState
		{
			public MicrowaveOpen(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "open";
				microwave.spriteHandler.ChangeSprite((int) SpriteState.Open);
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, true);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, true);
				microwave.HaltMicrowave();
				microwave.SetWattage(microwave.circuitWattage + microwave.ovenBulbWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				if (microwave.TryTransferToMicrowave(interaction) == false)
				{
					microwave.SetState(new MicrowaveIdle(microwave));
				}
			}

			public override void AddTime(int seconds)
			{
				microwave.AddTime(seconds);
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					microwave.SetState(new MicrowaveUnpoweredOpen(microwave));
				}
			}
		}

		private class MicrowaveRunning : MicrowaveState
		{
			public MicrowaveRunning(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "running";
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, true);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, true);
				microwave.spriteHandler.ChangeSprite((int) SpriteState.Running);
				microwave.SetWattage(microwave.circuitWattage + microwave.ovenBulbWattage + microwave.magnetronWattage);
			}

			public override void ToggleActive()
			{
				microwave.SetState(new MicrowaveIdle(microwave));
			}

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				microwave.OpenMicrowaveAndEjectContents();
				microwave.SetState(new MicrowaveOpen(microwave));
			}

			public override void AddTime(int seconds)
			{
				microwave.AddTime(seconds);
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					microwave.SetState(new MicrowaveUnpowered(microwave));
				}
			}
		}

		private class MicrowaveUnpowered : MicrowaveState
		{
			public MicrowaveUnpowered(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "unpowered";
				microwave.spriteHandler.ChangeSprite((int) SpriteState.Unpowered);
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, false);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, false);
				microwave.HaltMicrowave();
				microwave.SetWattage(microwave.circuitWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				microwave.OpenMicrowaveAndEjectContents();
				microwave.SetState(new MicrowaveUnpoweredOpen(microwave));
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state)
			{
				if (state != PowerState.Off)
				{
					microwave.SetState(new MicrowaveIdle(microwave));
				}
			}
		}

		private class MicrowaveUnpoweredOpen : MicrowaveState
		{
			public MicrowaveUnpoweredOpen(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "unpowered and open";
				microwave.spriteHandler.ChangeSprite((int) SpriteState.UnpoweredOpen);
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, false);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, false);
				microwave.HaltMicrowave();
				microwave.SetWattage(microwave.circuitWattage + microwave.ovenBulbWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				if (microwave.TryTransferToMicrowave(interaction) == false)
				{
					microwave.SetState(new MicrowaveUnpowered(microwave));
				}
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state)
			{
				if (state != PowerState.Off)
				{
					microwave.SetState(new MicrowaveOpen(microwave));
				}
			}
		}

		// Screwdriver to fix (reassemble) microwave
		private class MicrowaveBroken : MicrowaveState
		{
			public MicrowaveBroken(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "broken";
				microwave.spriteHandler.ChangeSprite((int) SpriteState.Broken);
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, false);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, false);
				microwave.HaltMicrowave();
				microwave.SetWattage(microwave.circuitWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				microwave.OpenMicrowaveAndEjectContents();
				microwave.SetState(new MicrowaveBrokenOpen(microwave));
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state) { }
		}

		private class MicrowaveBrokenOpen : MicrowaveState
		{
			public MicrowaveBrokenOpen(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "broken and open";
				microwave.spriteHandler.ChangeSprite((int) SpriteState.BrokenOpen);
				microwave.OnSyncScreenGlow(microwave.screenGlowEnabled, false);
				microwave.OnSyncOvenGlow(microwave.ovenGlowEnabled, false);
				microwave.HaltMicrowave();
				microwave.SetWattage(microwave.circuitWattage);
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(PositionalHandApply interaction)
			{
				if (microwave.TryTransferToMicrowave(interaction) == false)
				{
					microwave.SetState(new MicrowaveBroken(microwave));
				}
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state) { }
		}

		#endregion
	}
}

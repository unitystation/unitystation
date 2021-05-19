using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;
using AddressableReferences;
using Audio.Containers;
using Items;
using Machines;
using Messages.Server.SoundMessages;
using Objects.Machines;

namespace Objects.Kitchen
{
	/// <summary>
	/// A machine into which players can insert items for cooking. If the item has the Cookable component,
	/// the item will be cooked once enough time has lapsed as determined in that component.
	/// Otherwise, any food item that doesn't have the cookable component will be cooked using
	/// the legacy way, of converting to cooked when the microwave's timer finishes.
	/// </summary>
	public class Microwave : NetworkBehaviour, IAPCPowerable
	{
		private const int MAX_TIMER_TIME = 60; // Seconds
		private const float DIRTY_CHANCE_PER_FINISH = 10; // Percent

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

		/// <summary>
		/// How much time remains on the microwave's timer.
		/// </summary>
		[NonSerialized]
		public float microwaveTimer = 0;

		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private ItemStorage storage;
		[NonSerialized]
		public ItemSlot storageSlot;
		private Cookable storedCookable;

		[SyncVar(hook = nameof(OnSyncPlayAudioLoop))]
		private bool playAudioLoop;

		public bool IsOperating => currentState is MicrowaveRunning;
		public bool HasContents => storageSlot.IsOccupied;
		public Vector3Int WorldPosition => registerTile.WorldPosition;

		public MicrowaveState currentState;

		// Stock Part Tier.
		private int laserTier;


		#region Lifecycle

		private void Awake()
		{
			EnsureInit();
		}

		private void EnsureInit()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			storage = GetComponent<ItemStorage>();

			SetState(new MicrowaveIdle(this));

			// Get the machine stock parts used in this instance and get the tier of each part.

			IDictionary<GameObject, int> builtParts = GetComponent<Machine>().PartsInFrame;

			ICollection<GameObject> parts = builtParts.Keys;

			ItemAttributesV2 partAttributes;

			// Collection is unorganized so run through the whole list.
			foreach (GameObject part in parts)
			{
				partAttributes = part.GetComponent<ItemAttributesV2>();
				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MicroLaser))
				{
					laserTier = part.GetComponent<StockTier>().Tier;
				}

				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MatterBin))
				{
					int binTier = part.GetComponent<StockTier>().Tier;

					// Decide ItemStorageStructure based on tier. Currently: slot size == matter bin tier.
					storage.AcceptNewStructure(TierStorage[binTier-1]);
				}
			}
		}

		private void Start()
		{
			microwaveTimer = DefaultTimerTime;
			storageSlot = storage.GetIndexedItemSlot(0);
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
			if (!IsOperating) return;

			microwaveTimer -= Time.deltaTime*LaserTierTimeEffect();

			if (microwaveTimer <= 0)
			{
				microwaveTimer = 0;
				MicrowaveTimerComplete();
			}

			CheckCooked();
		}

		/// <summary>
		/// The micro-laser's tier affects the speed in which the microwave counts down and the speed
		/// in which food is cooked. For each tier above one, cook time is decreased by a factor of 0.5;
		/// </summary>
		private float LaserTierTimeEffect()
		{
			return (float)Math.Pow(2, ((double)laserTier - 1));
		}

		/// <summary>
		/// Return the size of the storage.
		/// </summary>
		public int StorageSize()
		{
			return storage.StorageSize();
		}

		private void SetState(MicrowaveState newState)
		{
			currentState = newState;
		}

		#region Requests

		/// <summary>
		/// Starts or stops the microwave, depending on the microwave's current state.
		/// </summary>
		public void RequestToggleActive()
		{
			currentState.ToggleActive();
		}

		/// <summary>
		/// Opens or closes the microwave's door, depending on the microwave's current state.
		/// If closing, will attempt to add the currently held item to the microwave.
		/// </summary>
		/// <param name="fromSlot">The slot with which to check if an item is held, for inserting an item when closing.</param>
		public void RequestDoorInteraction(ItemSlot fromSlot = null)
		{
			currentState.DoorInteraction(fromSlot);
		}

		/// <summary>
		/// Adds the given time to the microwave's timer. Can be negative.
		/// </summary>
		/// <param name="seconds">The amount of time, in seconds, to add to the timer. Can be negative.</param>
		public void RequestAddTime(int seconds)
		{
			currentState.AddTime(seconds);
		}

		#endregion

		private void TransferToMicrowaveAndClose(ItemSlot fromSlot)
		{
			if (fromSlot == null || fromSlot.IsEmpty) return;
			if (!Inventory.ServerTransfer(fromSlot, storage.GetNextFreeIndexedSlot())) return;
			if (storageSlot.ItemObject.TryGetComponent(out Cookable cookable))
			{
				storedCookable = cookable;
			}
		}

		private void OpenMicrowaveAndEjectContents()
		{
			SoundManager.PlayNetworkedAtPos(doorSFX.GetRandomClip(), WorldPosition, sourceObj: gameObject);

			Vector2 spritePosWorld = spriteHandler.transform.position;
			Vector2 microwaveInteriorCenterAbs = spritePosWorld + new Vector2(-0.075f, -0.075f);
			Vector2 microwaveInteriorCenterRel = microwaveInteriorCenterAbs - WorldPosition.To2Int();

			// Looks nicer if we drop the item in the middle of the sprite's representation of the microwave's interior.

			foreach (var slot in storage.GetItemSlots())
			{
				if (slot.IsOccupied == true)
				{
					Inventory.ServerDrop(slot, microwaveInteriorCenterRel);
				}
			}

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
			var potentialValue = microwaveTimer + seconds;

			if (potentialValue < MAX_TIMER_TIME && potentialValue > 0)
			{
				microwaveTimer += seconds;
			}
			AudioSourceParameters audioSourceParameters = new AudioSourceParameters(pitch: seconds < 0 ? 0.8f : 1);
			SoundManager.PlayNetworkedAtPos(timerBeepSFX, WorldPosition, audioSourceParameters, sourceObj: gameObject);
		}

		private void MicrowaveTimerComplete()
		{
			HaltMicrowave();
			SoundManager.PlayNetworkedAtPos(doneDingSFX, WorldPosition, sourceObj: gameObject);

			// Chance to dirty microwave. Could probably tie this tied into what is cooked instead or additionally.
			if (UnityEngine.Random.Range(1, 101) < DIRTY_CHANCE_PER_FINISH)
			{
				DirtyMicrowave();
			}

			LegacyFinishCooking();
			microwaveTimer = DefaultTimerTime;

			SetState(new MicrowaveIdle(this));
		}

		private void CheckCooked()
		{
			/* -- Obsolete, only affects one item.
			if (storedCookable == null) return;

			// True if the item's total cooking time exceeds the item's minimum cooking time.
			if (storedCookable.AddCookingTime(Time.deltaTime*LaserTierTimeEffect()))
			{
				// Swap item for its cooked version, if applicable.

				if (storedCookable.CookedProduct == null) return;

				Despawn.ServerSingle(storedCookable.gameObject);
				GameObject cookedItem = Spawn.ServerPrefab(storedCookable.CookedProduct).GameObject;
				Inventory.ServerAdd(cookedItem, storageSlot);
			}
			*/

			foreach (var slot in storage.GetItemSlots())
			{
				if (slot.IsOccupied == true)
				{

					if (slot.ItemObject.TryGetComponent(out Cookable slotCooked))
					{

						// True if the item's total cooking time exceeds the item's minimum cooking time.
						if (slotCooked.AddCookingTime(Time.deltaTime * LaserTierTimeEffect()) == true)
						{
							// Swap item for its cooked version, if applicable.
							if (slotCooked.CookedProduct == null) return;

							_ = Despawn.ServerSingle(slotCooked.gameObject);
							GameObject cookedItem = Spawn.ServerPrefab(slotCooked.CookedProduct).GameObject;
							Inventory.ServerAdd(cookedItem, slot);
						}

					}

				}

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

		// We delay the running SFX so the starting SFX has time to play.
		private IEnumerator DelayMicrowaveRunningSFX()
		{
			yield return WaitFor.Seconds(0.25f);

			// Check to make sure the state hasn't changed in the meantime.
			if (playAudioLoop)
			{
				runLoopGUID = Guid.NewGuid().ToString();
				SoundManager.PlayAtPositionAttached(RunningAudio, registerTile.WorldPosition, gameObject, runLoopGUID);
			}
		}

		#region LegacyCode
		/* This region is legacy, because it relies on the timer ending to cook the food.
		 * It also uses a different system for cooking; the crafting system and not via the Cookable component.
		 */

		public GameObject GetMeal(GameObject item)
		{
			ItemAttributesV2 itemAttributes = item.GetComponent<ItemAttributesV2>();
			Ingredient ingredient = new Ingredient(itemAttributes.ArticleName);
			return CraftingManager.Meals.FindRecipe(new List<Ingredient> { ingredient });
		}

		/// <summary>
		/// Finish cooking the microwaved meal.
		/// </summary>
		private void LegacyFinishCooking()
		{
			if (!HasContents) return;

			foreach (var slot in storage.GetItemSlots())
			{
				if (slot.IsEmpty == true) continue;

				var item = slot.ItemObject;

				// HACK: Currently DOES NOT check how many items are used per meal
				// Blindly assumes each single item in a stack produces a meal

				//If food item is stackable, set output amount to equal input amount.
				int originalStackAmount = 1;
				if (item.TryGetComponent(out Stackable stackable))
				{
					originalStackAmount = stackable.Amount;
				}

				GameObject itemToSpawn;

				// Check if the microwave could cook the food.
				GameObject meal = GetMeal(item);
				if (meal == null) return;

				itemToSpawn = CraftingManager.Meals.FindOutputMeal(meal.name);
				GameObject spawned = Spawn.ServerPrefab(itemToSpawn, WorldPosition).GameObject;

				if (spawned.TryGetComponent(out Stackable newItemStack))
				{
					int mealDeficit = originalStackAmount - newItemStack.InitialAmount;
					while (mealDeficit > 0)
					{
						mealDeficit = newItemStack.ServerIncrease(mealDeficit);
					}
				}

				_ = Despawn.ServerSingle(item);
				Inventory.ServerAdd(spawned, slot);
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
			EnsureInit(); // This method could be called before the component's Awake().
			currentState.PowerStateUpdate(state);
		}

		public void PowerNetworkUpdate(float voltage) { }

		#endregion

		#region MicrowaveStates

		public abstract class MicrowaveState
		{
			public string StateMsgForExamine = "an unknown state";
			protected Microwave microwave;

			public abstract void ToggleActive();
			public abstract void DoorInteraction(ItemSlot fromSlot);
			public abstract void AddTime(int seconds);
			public abstract void PowerStateUpdate(PowerState state);
		}

		private class MicrowaveIdle : MicrowaveState
		{
			public MicrowaveIdle(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "idle";
				microwave.spriteHandler.ChangeSprite(0);
				microwave.ScreenGlow.SetActive(true);
				microwave.OvenGlow.SetActive(false);
				microwave.HaltMicrowave();
			}

			public override void ToggleActive()
			{
				microwave.StartMicrowave();
				microwave.SetState(new MicrowaveRunning(microwave));
			}

			public override void DoorInteraction(ItemSlot fromSlot)
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
				if (state == PowerState.Off || state == PowerState.LowVoltage)
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
				microwave.spriteHandler.ChangeSprite(1);
				microwave.ScreenGlow.SetActive(true);
				microwave.OvenGlow.SetActive(true);
				microwave.HaltMicrowave();
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(ItemSlot fromSlot)
			{
				SoundManager.PlayNetworkedAtPos(microwave.doorSFX.GetRandomClip(), microwave.WorldPosition, sourceObj: microwave.gameObject);

				// Close if nothing's in hand.
				if (fromSlot == null || fromSlot.Item == null)
				{
					microwave.SetState(new MicrowaveIdle(microwave));
					return;
				}

				microwave.TransferToMicrowaveAndClose(fromSlot);

				// If storage is full, close.
				bool isFull = true;
				foreach (var slot in microwave.storage.GetItemSlots())
				{
					if (slot.IsEmpty == true)
					{
						isFull = false;
					}
				}

				if (isFull == true)
					microwave.SetState(new MicrowaveIdle(microwave));
			}

			public override void AddTime(int seconds)
			{
				microwave.AddTime(seconds);
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off || state == PowerState.LowVoltage)
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
				microwave.ScreenGlow.SetActive(true);
				microwave.OvenGlow.SetActive(true);
				microwave.spriteHandler.ChangeSprite(2);
			}

			public override void ToggleActive()
			{
				microwave.SetState(new MicrowaveIdle(microwave));
			}

			public override void DoorInteraction(ItemSlot fromSlot)
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
				if (state == PowerState.Off || state == PowerState.LowVoltage)
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
				microwave.spriteHandler.ChangeSprite(3);
				microwave.ScreenGlow.SetActive(false);
				microwave.OvenGlow.SetActive(false);
				microwave.HaltMicrowave();
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(ItemSlot fromSlot)
			{
				microwave.OpenMicrowaveAndEjectContents();
				microwave.SetState(new MicrowaveUnpoweredOpen(microwave));
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.On || state == PowerState.OverVoltage)
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
				microwave.spriteHandler.ChangeSprite(4);
				microwave.ScreenGlow.SetActive(false);
				microwave.OvenGlow.SetActive(false);
				microwave.HaltMicrowave();
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(ItemSlot fromSlot)
			{
				// Close if nothing's in hand.
				if (fromSlot.Item == null)
				{
					microwave.SetState(new MicrowaveUnpowered(microwave));
					return;
				}

				microwave.TransferToMicrowaveAndClose(fromSlot);

				// If storage is full, close.
				bool isFull = true;
				foreach (var slot in microwave.storage.GetItemSlots())
				{
					if (slot.IsEmpty == true)
					{
						isFull = false;
					}
				}

				if (isFull == true)
					microwave.SetState(new MicrowaveUnpowered(microwave));
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.On || state == PowerState.OverVoltage)
				{
					microwave.SetState(new MicrowaveOpen(microwave));
				}
			}
		}

		// Note: Currently no way to enter or escape broken states.
		private class MicrowaveBroken : MicrowaveState
		{
			public MicrowaveBroken(Microwave microwave)
			{
				this.microwave = microwave;
				StateMsgForExamine = "broken";
				microwave.spriteHandler.ChangeSprite(5);
				microwave.ScreenGlow.SetActive(false);
				microwave.OvenGlow.SetActive(false);
				microwave.HaltMicrowave();
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(ItemSlot fromSlot)
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
				microwave.spriteHandler.ChangeSprite(6);
				microwave.ScreenGlow.SetActive(false);
				microwave.OvenGlow.SetActive(false);
				microwave.HaltMicrowave();
			}

			public override void ToggleActive() { }

			public override void DoorInteraction(ItemSlot fromSlot)
			{
				// Close if nothing's in hand.
				if (fromSlot.Item == null)
				{
					microwave.SetState(new MicrowaveBroken(microwave));
					return;
				}

				microwave.TransferToMicrowaveAndClose(fromSlot);

				// If storage is full, close.
				bool isFull = true;
				foreach (var slot in microwave.storage.GetItemSlots())
				{
					if (slot.IsEmpty == true)
					{
						isFull = false;
					}
				}

				if (isFull == true)
					microwave.SetState(new MicrowaveBroken(microwave));
			}

			public override void AddTime(int seconds) { }

			public override void PowerStateUpdate(PowerState state) { }
		}

		#endregion
	}
}

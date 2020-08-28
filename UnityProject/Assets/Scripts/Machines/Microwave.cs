using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/// <summary>
/// A machine into which players can insert items for cooking. If the item has the Cookable component,
/// the item will be cooked once enough time has lapsed as determined in that component.
/// Otherwise, any food item that doesn't have the cookable component will be cooked using
/// the legacy way, of converting to cooked when the microwave's timer finishes.
/// </summary>
public class Microwave : NetworkBehaviour, IAPCPowered
{
	private const int MAX_TIMER_TIME = 60; // Seconds
	private const float DIRTY_CHANCE_PER_FINISH = 10; // Percent
	private const string DOOR_SOUND = "Punch#"; // SFX the microwave door should make when opening/closing.
	private const string TIMER_BEEP = "Beep"; // Beep to play when timer time is added/removed.

	[SerializeField]
	[Range(0, MAX_TIMER_TIME)]
	[Tooltip("Default time the microwave should reset to when finished (in seconds). Should be set to a value that will cook most items.")]
	private float DefaultTimerTime = 10;

	[SerializeField]
	[Tooltip("The looped audio source to play while the microwave is running.")]
	private AudioSource RunningAudio = default;

	[SerializeField]
	[Tooltip("Child GameObject that is responsible for the screen glow.")]
	private GameObject ScreenGlow = default;

	[SerializeField]
	[Tooltip("Child GameObject that is responsible for the glow from the interior oven when the door is open.")]
	private GameObject OvenGlow = default;

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

	#endregion Lifecycle

	/// <summary>
	/// Reduce the microwave's timer, add that time to food cooking.
	/// </summary>
	private void UpdateMe()
	{
		if (!IsOperating) return;

		microwaveTimer -= Time.deltaTime;

		if (microwaveTimer <= 0)
		{
			microwaveTimer = 0;
			MicrowaveTimerComplete();
		}

		CheckCooked();
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

	/// <summary>
	/// Part of interface IAPCPowered. Sets the microwave's state according to the given power state.
	/// </summary>
	/// <param name="state">The power state to set the microwave's state with.</param>
	public void StateUpdate(PowerStates state)
	{
		EnsureInit(); // This method could be called before the component's Awake().
		currentState.PowerStateUpdate(state);
	}

	#endregion Requests

	private void TransferToMicrowaveAndClose(ItemSlot fromSlot)
	{
		if (fromSlot != null && fromSlot.ItemObject != null)
		{
			Inventory.ServerTransfer(fromSlot, storageSlot);
			if (storageSlot.ItemObject.TryGetComponent(out Cookable cookable))
			{
				storedCookable = cookable;
			}
		}

		SoundManager.PlayNetworkedAtPos(DOOR_SOUND, WorldPosition, sourceObj: gameObject);
	}

	private void OpenMicrowaveAndEjectContents()
	{
		SoundManager.PlayNetworkedAtPos(DOOR_SOUND, WorldPosition, sourceObj: gameObject);

		Vector2 spritePosWorld = spriteHandler.transform.position;
		Vector2 microwaveInteriorCenterAbs = spritePosWorld + new Vector2(-0.075f, -0.075f);
		Vector2 microwaveInteriorCenterRel = microwaveInteriorCenterAbs - WorldPosition.To2Int();

		// Looks nicer if we drop the item in the middle of the sprite's representation of the microwave's interior.
		Inventory.ServerDrop(storageSlot, microwaveInteriorCenterRel);
		storedCookable = null;
	}

	private void StartMicrowave()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		SoundManager.PlayNetworkedAtPos("MicrowaveStart", WorldPosition, sourceObj: gameObject);
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

		SoundManager.PlayNetworkedAtPos(TIMER_BEEP, WorldPosition, sourceObj: gameObject, pitch: seconds < 0 ? 0.8f : 1);
	}

	private void MicrowaveTimerComplete()
	{
		HaltMicrowave();
		SoundManager.PlayNetworkedAtPos("MicrowaveDing", WorldPosition, sourceObj: gameObject);

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
		if (storedCookable == null) return;

		// True if the item's total cooking time exceeds the item's minimum cooking time.
		if (storedCookable.AddCookingTime(Time.deltaTime))
		{
			// Swap item for its cooked version, if applicable.

			if (storedCookable.CookedProduct == null) return;

			Despawn.ServerSingle(storedCookable.gameObject);
			GameObject cookedItem = Spawn.ServerPrefab(storedCookable.CookedProduct).GameObject;
			Inventory.ServerAdd(cookedItem, storageSlot);
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
			RunningAudio.Stop();
		}
	}

	// We delay the running SFX so the starting SFX has time to play.
	private IEnumerator DelayMicrowaveRunningSFX()
	{
		yield return WaitFor.Seconds(0.25f);

		// Check to make sure the state hasn't changed in the meantime.
		if (playAudioLoop) RunningAudio.Play();
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
		var item = storageSlot.ItemObject;

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

		Despawn.ServerSingle(item);
		Inventory.ServerAdd(spawned, storageSlot);
	}

	#endregion LegacyCode

	public void PowerNetworkUpdate(float Voltage)
	{
		return;
	}

	#region MicrowaveStates

	public abstract class MicrowaveState
	{
		public string StateMsgForExamine = "an unknown state";
		protected Microwave microwave;

		public abstract void ToggleActive();
		public abstract void DoorInteraction(ItemSlot fromSlot);
		public abstract void AddTime(int seconds);
		public abstract void PowerStateUpdate(PowerStates state);
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

		public override void PowerStateUpdate(PowerStates state)
		{
			if (state == PowerStates.Off || state == PowerStates.LowVoltage)
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
			microwave.TransferToMicrowaveAndClose(fromSlot);
			microwave.SetState(new MicrowaveIdle(microwave));
		}

		public override void AddTime(int seconds)
		{
			microwave.AddTime(seconds);
		}

		public override void PowerStateUpdate(PowerStates state)
		{
			if (state == PowerStates.Off || state == PowerStates.LowVoltage)
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

		public override void PowerStateUpdate(PowerStates state)
		{
			if (state == PowerStates.Off || state == PowerStates.LowVoltage)
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

		public override void PowerStateUpdate(PowerStates state)
		{
			if (state == PowerStates.On || state == PowerStates.OverVoltage)
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
			microwave.TransferToMicrowaveAndClose(fromSlot);
			microwave.SetState(new MicrowaveUnpowered(microwave));
		}

		public override void AddTime(int seconds) { }

		public override void PowerStateUpdate(PowerStates state)
		{
			if (state == PowerStates.On || state == PowerStates.OverVoltage)
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

		public override void PowerStateUpdate(PowerStates state) { }
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
			microwave.TransferToMicrowaveAndClose(fromSlot);
			microwave.SetState(new MicrowaveBroken(microwave));
		}

		public override void AddTime(int seconds) { }

		public override void PowerStateUpdate(PowerStates state) { }
	}

	#endregion MicrowaveStates
}

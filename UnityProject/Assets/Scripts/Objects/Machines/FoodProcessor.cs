using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Systems.Electricity;
using AddressableReferences;
using Effects;
using Items;
using Machines;
using Objects.Machines;

namespace Objects.Kitchen
{
	// TODO: needs sounds

	/// <summary>
	/// A machine into which players can insert items for cooking. If the item has the Cookable component,
	/// the item will be cooked once enough time has lapsed as determined in that component.
	/// Otherwise, any food item that doesn't have the cookable component will be cooked using
	/// the legacy way, of converting to cooked when the processor's timer finishes.
	/// </summary>
	public class FoodProcessor : NetworkBehaviour, IAPCPowerable, IServerSpawn
	{
		private const int TIME_PER_ITEM = 4;

		[SerializeField]
		[Tooltip("The looped audio source to play while the processor is running.")]
		private AddressableAudioSource RunningAudio = null;

		private string runLoopGUID = "";

		/// <summary>
		/// How much time remains on the processor's timer.
		/// </summary>
		[NonSerialized]
		public float processTimer = 0;

		private RegisterTile registerTile;
		private SpriteHandler spriteHandler;
		private ItemStorage storage;

		[SyncVar(hook = nameof(OnSyncPlayAudioLoop))]
		private bool playAudioLoop;

		public bool IsOperating => currentState is ProcessorRunning;
		public Vector3Int WorldPosition => registerTile.WorldPosition;

		public ProcessorState currentState = null;

		[SerializeField]
		private Shake shaker;
		// For shake animation
		private float shakeValue = 0.03f;

		// Checks the first unfilled slot. If the first slot is unfilled then the processor
		// must be empty. (Always adds starting from the first slot, and emptying always
		// empties the entire inventory.)
		public bool IsFilled => (storage.GetNextFreeIndexedSlot().SlotIdentifier.SlotIndex != 0);

		// Stock Part Tier.
		private int manipTier = 1;
		private int binTier = 1;

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			storage = GetComponent<ItemStorage>();
		}

		public void OnSpawnServer(SpawnInfo spawn)
		{
			SetState(new ProcessorUnpowered(this));

			// Get the machine stock parts used in this instance and get the tier of each part.

			IDictionary<GameObject, int> builtParts = GetComponent<Machine>().PartsInFrame;

			ICollection<GameObject> parts = builtParts.Keys;

			ItemAttributesV2 partAttributes;

			// Collection is unorganized so run through the whole list.
			foreach (GameObject part in parts)
			{
				partAttributes = part.GetComponent<ItemAttributesV2>();
				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.Manipulator))
				{
					// Manipulator tier determines process speed. For each tier above 1,
					// the time it takes to process a single object is reduced by 1 second.
					manipTier = part.GetComponent<StockTier>().Tier;
				}

				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MatterBin))
				{
					// Bin tier determines product number. The amount of product
					// per input object is equal to the bin tier.
					binTier = part.GetComponent<StockTier>().Tier;
				}
			}
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion Lifecycle

		/// <summary>
		/// Count down processor timer.
		/// </summary>
		private void UpdateMe()
		{
			if (!IsOperating) return;

			processTimer -= Time.deltaTime;

			//AnimateProcessor(shakeValue);
			//shakeValue = -1 * shakeValue;

			if (processTimer <= 0)
			{
				processTimer = 0;
				ProcessTimerComplete();
			}
		}

		private void SetState(ProcessorState newState)
		{
			currentState = newState;
		}

		#region Requests

		/// <summary>
		/// Starts or stops the processor, depending on the processor's current state.
		/// </summary>
		public void RequestToggleActive()
		{
			currentState.ToggleActive();
		}

		/// <summary>
		/// Ejects the contents of the processor. Turns off the machine if it's on.
		/// </summary>
		public void RequestEjectContents()
		{
			currentState.EjectContents();
		}

		/// <summary>
		/// Attempts to add an item. If the machine is active turn it off instead.
		/// </summary>
		public void RequestAddItem(ItemSlot fromSlot)
		{
			currentState.AddItem(fromSlot);
		}

		#endregion Requests

		private void AddItem(ItemSlot fromSlot)
		{
			if (fromSlot == null || fromSlot.IsEmpty || fromSlot.ItemObject.GetComponent<Processable>() == null) return;

			// If there's a stackable component, add one at a time.
			Stackable stack = fromSlot.ItemObject.GetComponent<Stackable>();
			if (stack == null || stack.Amount == 1)
			{
				Inventory.ServerTransfer(fromSlot, storage.GetNextFreeIndexedSlot());
			}
			else {
				var item = stack.ServerRemoveOne();
				Inventory.ServerAdd(item, storage.GetNextFreeIndexedSlot());
			}
		}

		private void EjectContents()
		{
			storage.ServerDropAll();
		}

		private void StartProcessor()
		{
			// Process time is determined by the number of items inside and the tier of the manipulator.
			// time in seconds = 4 * items_in_processor / manipulator tier
			int slotsOccupied = storage.GetNextFreeIndexedSlot().SlotIdentifier.SlotIndex;
			processTimer = (float)(TIME_PER_ITEM * slotsOccupied / manipTier);
			AnimateProcessor(1, processTimer / 8, shakeValue, 0.1f);
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			playAudioLoop = true;

		}

		private void HaltProcessor()
		{
			if (IsOperating == false) return;

			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			AnimateProcessor(0, 0.0f, 0.0f, 0.0f);
			playAudioLoop = false;
		}

		[ClientRpc]
		private void RpcHaltProcessorAnim()
		{
			AnimateProcessor(0, 0.0f, 0.0f, 0.0f);
		}

		private void ProcessTimerComplete()
		{
			HaltProcessor();

			FinishProcessing();

			SetState(new ProcessorIdle(this));
		}
		/// <summary>
		/// Animates the food Processor using Shake.cs
		/// </summary>
		/// <param name="state">Checks if the animation should be stopped or enabled.</param>
		private void AnimateProcessor(int state, float duration, float distance, float delayBetweenShakes)
		{
			if(state == 1)
			{
				shaker.StartShake(duration, distance, delayBetweenShakes);

				if (CustomNetworkManager.IsServer)
				{
					RpcShake(duration, distance, delayBetweenShakes);
				}
			}
			else
			{
				shaker.HaltShake();

				if (CustomNetworkManager.IsServer)
				{
					RpcHaltProcessorAnim();
				}
			}
		}

		/// <summary>
		/// Send animation to clients.
		/// </summary>
		[ClientRpc]
		private void RpcShake(float duration, float distance, float delayBetweenShakes)
		{
			shaker.StartShake(duration, distance, delayBetweenShakes);
		}

		private void OnSyncPlayAudioLoop(bool oldState, bool newState)
		{
			if (newState)
			{
				runLoopGUID = Guid.NewGuid().ToString();
				SoundManager.PlayAtPositionAttached(RunningAudio, registerTile.WorldPosition, gameObject, runLoopGUID);
			}
			else
			{
				SoundManager.Stop(runLoopGUID);
			}
		}

		/// <summary>
		/// Process everything in processor inventory.
		/// </summary>
		private void FinishProcessing()
		{

			foreach (var slot in storage.GetItemSlots())
			{
				if (slot.IsEmpty == true) break;

				// If, somehow, something unprocessable is in the processor (should be impossible), spit it out.
				var processable = slot.ItemObject.GetComponent<Processable>();
				if (processable == null)
				{
					Inventory.ServerDrop(slot);
					continue;
				}

				var item = slot.ItemObject;

				GameObject itemToSpawn = processable.ProcessedProduct;

				// Very rarely an item will create multiple products at once, even without a matter bin upgrade.
				int itemCount = processable.ProductAmount;

				for (var i = 0; i < itemCount*binTier; ++i)
				{
					Spawn.ServerPrefab(itemToSpawn, WorldPosition);
				}

				_ = Despawn.ServerSingle(item);
			}

		}

		#region IAPCPowerable

		// Processor functionality doesn't care for voltage.
		public void PowerNetworkUpdate(float voltage) { }

		/// <summary>
		/// Part of interface IAPCPowerable. Sets the processor's state according to the given power state.
		/// </summary>
		/// <param name="state">The power state to set the processor's state with.</param>
		public void StateUpdate(PowerState state)
		{
			if (isServer) // Since state changes affect animations (and call an Rpc animation function), only server can do this.
			{
				if (currentState == null)
				{
					SetState(new ProcessorUnpowered(this));
				}
				currentState.PowerStateUpdate(state);
			}


		}

		#endregion

		#region ProcessorStates

		public abstract class ProcessorState
		{
			public string StateMsgForExamine = "an unknown state";
			protected FoodProcessor processor;

			public abstract void ToggleActive();
			public abstract void AddItem(ItemSlot fromSlot);
			public abstract void EjectContents();
			public abstract void PowerStateUpdate(PowerState state);
		}

		private class ProcessorIdle : ProcessorState
		{
			public ProcessorIdle(FoodProcessor processor)
			{
				this.processor = processor;
				StateMsgForExamine = "idle";
				processor.spriteHandler.ChangeSprite(0);
				processor.HaltProcessor();
			}

			public override void ToggleActive()
			{
				// Do not turn the processor on if it's empty.
				if (processor.IsFilled == false) return;

				processor.StartProcessor();
				processor.SetState(new ProcessorRunning(processor));
			}

			public override void AddItem(ItemSlot fromSlot)
			{
				processor.AddItem(fromSlot);
			}

			public override void EjectContents()
			{
				processor.EjectContents();
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off || state == PowerState.LowVoltage)
				{
					processor.SetState(new ProcessorUnpowered(processor));
					processor.EjectContents();
				}
			}
		}

		private class ProcessorRunning : ProcessorState
		{
			public ProcessorRunning(FoodProcessor processor)
			{
				this.processor = processor;
				StateMsgForExamine = "running";
				processor.spriteHandler.ChangeSprite(1);
			}

			public override void ToggleActive()	{ }

			public override void AddItem(ItemSlot fromSlot)
			{
				// Processor cannot be added to during operation.
			}

			public override void EjectContents()
			{
				processor.EjectContents();
				processor.SetState(new ProcessorIdle(processor));
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off || state == PowerState.LowVoltage)
				{
					processor.SetState(new ProcessorUnpowered(processor));
					processor.EjectContents();
				}
			}
		}

		private class ProcessorUnpowered : ProcessorState
		{
			public ProcessorUnpowered(FoodProcessor processor)
			{
				this.processor = processor;
				StateMsgForExamine = "unpowered";
				//processor.AnimateProcessor(0);
				processor.spriteHandler.ChangeSprite(0);
				processor.HaltProcessor();
			}

			public override void ToggleActive() { }

			public override void AddItem(ItemSlot fromSlot)
			{
				// Processor cannot accept anything if it's off.
			}

			public override void EjectContents()
			{
				processor.EjectContents();
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.On || state == PowerState.OverVoltage)
				{
					processor.SetState(new ProcessorIdle(processor));
				}
			}
		}

		#endregion
	}
}

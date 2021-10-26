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
	/// A machine which can have meat items placed on top to cook them.
	/// If the item has the Cookable component, the item will be cooked
	/// once enough time has lapsed as determined in that component.
	/// </summary>
	public class Griddle : NetworkBehaviour, IAPCPowerable, IRefreshParts
	{
		private enum SpriteState
		{
			Idle = 0,
			Running = 1
		}

		[SerializeField] private AddressableAudioSource startSfx = null;

		[SerializeField]
		[Tooltip("The looped audio source to play while the griddle is running.")]
		private AddressableAudioSource RunningAudio = default;

		private string runLoopGUID = "";

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the griddle's circuitry and display.")]
		private int circuitWattage = 5;

		[SerializeField, Foldout("Power Usages")]
		[Tooltip("Wattage of the griddle's internal heater (T1 laser stock part).")]
		private int magnetronWattage = 850;

		private RegisterTile registerTile;

		private Matrix Matrix => registerTile.Matrix;
		private SpriteHandler spriteHandler;
		private APCPoweredDevice poweredDevice;

		[SyncVar(hook = nameof(OnSyncPlayAudioLoop))]
		private bool playAudioLoop;

		public bool IsOperating => CurrentState is GriddleRunning;

		public Vector3Int WorldPosition => registerTile.WorldPosition;

		// Stock Part Tier.
		private int laserTier = 1;
		private float voltageModifier = 1;

		/// <summary>
		/// The micro-laser's tier affects the speed in which the griddle counts down and the speed
		/// in which food is cooked. For each tier above one, cook time is decreased by a factor of 0.5
		/// </summary>
		private float LaserModifier => Mathf.Pow(2, laserTier - 1);

		private float Effectiveness => LaserModifier * voltageModifier;

		public GriddleState CurrentState { get; private set; }

		#region Lifecycle

		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			spriteHandler = GetComponentInChildren<SpriteHandler>();
			poweredDevice = GetComponent<APCPoweredDevice>();

			SetState(new GriddleUnpowered(this));
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		}

		#endregion

		/// <summary>
		/// Reduce the griddle's timer, add that time to food cooking.
		/// </summary>
		private void UpdateMe()
		{
			if (IsOperating == false) return;

			float cookTime = Time.deltaTime * Effectiveness;

			CheckCooked(cookTime);
		}

		private void SetState(GriddleState newState)
		{
			CurrentState = newState;
		}

		#region Requests

		/// <summary>
		/// Starts or stops the griddle, depending on the griddle's current state.
		/// </summary>
		public void RequestToggleActive()
		{
			CurrentState.ToggleActive();
		}

		/// <summary>
		/// Drops an item onto the griddle.
		/// </summary>
		/// <param name="fromSlot">The slot with which to check if an item is held, for inserting an item when closing.</param>
		public void RequestDropItem(PositionalHandApply interaction = null)
		{
			TryPlaceItemOnGriddle(interaction);
		}

		#endregion

		private bool TryPlaceItemOnGriddle(PositionalHandApply interaction)
		{
			if (interaction == null || interaction.UsedObject == null)
			{
				return false;
			}
			Inventory.ServerDrop(interaction.HandSlot, interaction.TargetVector);
			return true;
		}

		private void StartGriddle()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			SoundManager.PlayNetworkedAtPos(startSfx, WorldPosition, sourceObj: gameObject);
			playAudioLoop = true;
		}

		private void HaltGriddle()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			SoundManager.PlayNetworkedAtPos(startSfx, WorldPosition, sourceObj: gameObject);
			playAudioLoop = false;
		}

		private void CheckCooked(float cookTime)
		{
			var itemsOnGrill = Matrix.Get<ObjectBehaviour>(registerTile.LocalPositionServer, ObjectType.Item, true)
				.Where(ob => ob != null && ob.gameObject != gameObject);
			foreach (var onGrill in itemsOnGrill)
			{
				if(onGrill.gameObject.TryGetComponent(out Cookable slotCooked) && slotCooked.CookableBy.HasFlag(CookSource.Griddle))
				{
					if (slotCooked.AddCookingTime(cookTime) == true)
					{
						// Swap item for its cooked version, if applicable.
						if (slotCooked.CookedProduct == null) return;
						Spawn.ServerPrefab(slotCooked.CookedProduct, slotCooked.gameObject.transform.position, transform.parent);
						_ = Despawn.ServerSingle(slotCooked.gameObject);
					}
				}
			}
		}

		private void OnSyncPlayAudioLoop(bool oldState, bool newState)
		{
			if (newState)
			{
				StartCoroutine(DelayGriddleRunningSfx());
			}
			else
			{
				SoundManager.Stop(runLoopGUID);
			}
		}

		// We delay the running Sfx so the starting Sfx has time to play.
		private IEnumerator DelayGriddleRunningSfx()
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
			// Get the machine stock parts used in this instance and get the tier of each part.
			// Collection is unorganized so run through the whole list.
			foreach (GameObject part in partsInFrame.Keys)
			{
				ItemAttributesV2 partAttributes = part.GetComponent<ItemAttributesV2>();
				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MicroLaser))
				{
					laserTier = part.GetComponent<StockTier>().Tier;
				}
			}
		}

		#endregion

		#region IAPCPowerable

		/// <summary>
		/// Part of interface IAPCPowerable. Sets the griddle's state according to the given power state.
		/// </summary>
		/// <param name="state">The power state to set the griddle's state with.</param>
		public void StateUpdate(PowerState state)
		{
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

		#region GriddleStates

		public abstract class GriddleState
		{
			public string StateMsgForExamine = "an unknown state";
			protected Griddle griddle;

			public abstract void ToggleActive();
			public abstract void PowerStateUpdate(PowerState state);
		}

		private class GriddleIdle : GriddleState
		{
			public GriddleIdle(Griddle griddle)
			{
				this.griddle = griddle;
				StateMsgForExamine = "idle";
				griddle.spriteHandler.ChangeSprite((int) SpriteState.Idle);
				griddle.HaltGriddle();
				griddle.SetWattage(griddle.circuitWattage);
			}

			public override void ToggleActive()
			{
				griddle.StartGriddle();
				griddle.SetState(new GriddleRunning(griddle));
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					griddle.SetState(new GriddleUnpowered(griddle));
				}
			}
		}

		private class GriddleRunning : GriddleState
		{
			public GriddleRunning(Griddle griddle)
			{
				this.griddle = griddle;
				StateMsgForExamine = "running";
				griddle.spriteHandler.ChangeSprite((int) SpriteState.Running);
				griddle.SetWattage(griddle.circuitWattage + griddle.magnetronWattage);
			}

			public override void ToggleActive()
			{
				griddle.SetState(new GriddleIdle(griddle));
			}

			public override void PowerStateUpdate(PowerState state)
			{
				if (state == PowerState.Off)
				{
					griddle.SetState(new GriddleUnpowered(griddle));
				}
			}
		}

		private class GriddleUnpowered : GriddleState
		{
			public GriddleUnpowered(Griddle griddle)
			{
				this.griddle = griddle;
				StateMsgForExamine = "unpowered";
				griddle.spriteHandler.ChangeSprite((int) SpriteState.Idle);
				griddle.HaltGriddle();
				griddle.SetWattage(griddle.circuitWattage);
			}

			public override void ToggleActive() { }

			public override void PowerStateUpdate(PowerState state)
			{
				if (state != PowerState.Off)
				{
					griddle.SetState(new GriddleIdle(griddle));
				}
			}
		}

		#endregion
	}
}

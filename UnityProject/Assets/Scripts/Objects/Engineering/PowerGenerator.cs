using System;
using System.Collections;
using System.Collections.Generic;
using AddressableReferences;
using UnityEngine;
using Mirror;
using NaughtyAttributes;
using Systems.Electricity;
using Systems.Electricity.NodeModules;
using Objects.Construction;

namespace Objects.Engineering
{
	public class PowerGenerator : NetworkBehaviour, ICheckedInteractable<HandApply>, INodeControl, IExaminable, IServerSpawn
	{
		[Tooltip("Whether this generator should start running when spawned.")]
		[SerializeField]
		private bool startAsOn = false;

		[Tooltip("The rate of fuel this generator should consume.")]
		[Range(0.01f, 0.1f)]
		[SerializeField]
		private float fuelConsumptionRate = 0.02f;

		[Tooltip("The types of fuel this generator can consume (traits).")]
		[SerializeField]
		private List<ItemTrait> fuelTypes = null;

		private RegisterTile registerTile;
		private ItemSlot itemSlot;
		private WrenchSecurable securable;
		private SpriteHandler baseSpriteHandler;
		private ElectricalNodeControl electricalNodeControl;

		[SerializeField]
		private AddressableAudioSource generatorRunSfx = null;
		[SerializeField]
		private AddressableAudioSource generatorEndSfx = null;
		[SerializeField]
		private ParticleSystem smokeParticles = default;

		[SyncVar(hook = nameof(OnSyncState))]
		private bool isOn;
		private float fuelAmount;
		private float fuelPerSheet = 10f;

		private string runLoopGUID = "";

		private enum SpriteState
		{
			Unsecured = 0,
			Off = 1,
			On = 2
		}

		public float FuelAmount => fuelAmount;

		#region Lifecycle

		void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			securable = GetComponent<WrenchSecurable>();
			baseSpriteHandler = GetComponentInChildren<SpriteHandler>();
			electricalNodeControl = GetComponent<ElectricalNodeControl>();
			var itemStorage = GetComponent<ItemStorage>();
			itemSlot = itemStorage.GetIndexedItemSlot(0);
			securable.OnAnchoredChange.AddListener(OnSecuredChanged);
		}

		public void OnSpawnServer(SpawnInfo info)
		{
			if (startAsOn)
			{
				fuelAmount = fuelPerSheet;
				TryToggleOn();
			}
		}


		private void OnDisable()
		{
			if (isOn)
			{
				ToggleOff();
			}
		}

		#endregion Lifecycle

		public void PowerNetworkUpdate() { }

		private void OnSecuredChanged()
		{
			if (securable.IsAnchored)
			{
				baseSpriteHandler.ChangeSprite((int)SpriteState.Off);
			}
			else
			{
				ToggleOff();
				baseSpriteHandler.ChangeSprite((int)SpriteState.Unsecured);
			}

			ElectricalManager.Instance.electricalSync.StructureChange = true;
		}

		private void OnSyncState(bool oldState, bool newState)
		{
			isOn = newState;
			if (isOn)
			{
				baseSpriteHandler.PushTexture();
				smokeParticles.Play();
				runLoopGUID = Guid.NewGuid().ToString();
				SoundManager.PlayAtPositionAttached(generatorRunSfx, registerTile.WorldPosition, gameObject, runLoopGUID);
			}
			else
			{
				SoundManager.Stop(runLoopGUID);
				smokeParticles.Stop();
				_ = SoundManager.PlayAtPosition(generatorEndSfx, registerTile.WorldPosition, gameObject);
			}
		}

		#region Interaction

		public string Examine(Vector3 worldPos = default)
		{
			var examineText = $"The generator is {(HasFuel() ? "fueled" : "unfueled")} and " +
									$"{(isOn ? "running" : "not running")}. ";
			if (itemSlot.Item)
			{
				var stackable = itemSlot.Item.GetComponent<Stackable>();
				examineText += $"There's {stackable.Amount} sheets left in the storage compartment.";
			}
			else
			{
				examineText += $"There's no more sheets left in the storage compartment.";
			}
			return examineText;
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;
			if (interaction.TargetObject != gameObject) return false;
			if (interaction.HandObject == null) return true;
			if (Validations.HasAnyTrait(interaction.HandObject, fuelTypes)) return true;

			return false;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			if (interaction.HandObject == null && securable.IsAnchored)
			{
				if (!isOn)
				{
					if (!TryToggleOn())
					{
						Chat.AddWarningMsgFromServer(interaction.Performer, $"The generator needs more fuel!");
					}
				}
				else
				{
					ToggleOff();
				}
			}
			else
			{
				foreach (ItemTrait fuelType in fuelTypes)
				{
					if (Validations.HasItemTrait(interaction.HandObject, fuelType))
					{
						int amountTransfered;
						var handStackable = interaction.HandObject.GetComponent<Stackable>();
						if (itemSlot.Item)
						{
							var stackable = itemSlot.Item.GetComponent<Stackable>();
							if (stackable.SpareCapacity == 0)
							{
								Chat.AddWarningMsgFromServer(interaction.Performer, "The generator sheet storage is full!");
								return;
							}
							amountTransfered = stackable.ServerCombine(handStackable);
						}
						else
						{
							amountTransfered = handStackable.Amount;
							Inventory.ServerTransfer(interaction.HandSlot, itemSlot, ReplacementStrategy.DropOther);
						}
						Chat.AddExamineMsgFromServer(interaction.Performer, $"You fill the generator sheet storage with {amountTransfered.ToString()} more.");
					}
				}
			}
		}


		#endregion Interaction

		private void UpdateMe()
		{
			fuelAmount -= Time.deltaTime * fuelConsumptionRate;
			if (fuelAmount <= 0)
			{
				ConsumeSheet();
			}
		}

		private void ConsumeSheet()
		{
			if (Inventory.ServerConsume(itemSlot, 1))
			{
				fuelAmount += fuelPerSheet;
			}
			else
			{
				ToggleOff();
			}
		}

		private bool TryToggleOn()
		{
			if (fuelAmount > 0 || itemSlot.Item)
			{
				ToggleOn();
				return true;
			}
			return false;
		}

		private bool HasFuel()
		{
			if (fuelAmount > 0 || itemSlot.Item)
			{
				return true;
			}
			return false;
		}

		public void ToggleOn()
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			electricalNodeControl.TurnOnSupply();
			baseSpriteHandler.ChangeSprite((int)SpriteState.On);
			isOn = true;
		}

		private void ToggleOff()
		{
			UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
			electricalNodeControl.TurnOffSupply();
			baseSpriteHandler.ChangeSprite((int)SpriteState.Off);
			isOn = false;
		}

		public void SetFuel(float amount)
		{
			fuelAmount = amount;
		}

		[Button()]
		public void DebugAddFuel()
		{
			SetFuel(fuelAmount + fuelPerSheet);
		}
	}
}

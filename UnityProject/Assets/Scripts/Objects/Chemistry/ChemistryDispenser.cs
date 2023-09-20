using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry.Components;
using Mirror;
using UnityEngine;
using Systems.Electricity;
using UnityEngine.Serialization;

namespace Chemistry
{
	/// <summary>
	/// Main component for chemistry dispenser.
	/// </summary>
	public class ChemistryDispenser : NetworkBehaviour, ICheckedInteractable<HandApply>, IAPCPowerable
	{
		//yes This is a weird script
		//if you want the functionality it's all in GUI_ChemistryDispenser for some bizarre reason

		[FormerlySerializedAs("DispensedTemperature")]
		public float DispensedTemperatureCelsius = TemperatureUtils.ToKelvin(30,TemeratureUnits.C);

		private float PowerDrawHeating = 1500f;
		public bool HeaterOn = false;

		public float HeaterTemperatureKelvin = TemperatureUtils.ToKelvin(20f, TemeratureUnits.C);


		public ReagentContainer Container => itemSlot != null && itemSlot.ItemObject != null
			? itemSlot.ItemObject.GetComponent<ReagentContainer>()
			: null;

		public delegate void ChangeEvent();

		public static event ChangeEvent changeEvent;

		private ItemStorage itemStorage;
		private ItemSlot itemSlot;


		private APCPoweredDevice APCPoweredDevice;


		private void OnEnable()
		{
			UpdateManager.Add(HeaterUpdate, 1);
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, HeaterUpdate);
		}

		private void Awake()
		{
			APCPoweredDevice = GetComponent<APCPoweredDevice>();
			itemStorage = GetComponent<ItemStorage>();
			itemSlot = itemStorage.GetIndexedItemSlot(0);
		}

		private void UpdateGUI()
		{
			// Change event runs updateAll in GUI_ChemistryDispenser
			if (changeEvent != null)
			{
				changeEvent();
			}
		}


		private ItemSlot GetBestSlot(GameObject item, PlayerInfo subject)
		{
			if (subject == null)
			{
				return default;
			}

			var playerStorage = subject.Script.DynamicItemStorage;
			return playerStorage.GetBestHandOrSlotFor(item);
		}

		public void EjectContainer(PlayerInfo player)
		{
			if (!Inventory.ServerTransfer(itemSlot, GetBestSlot(itemSlot.ItemObject, player)))
			{
				Inventory.ServerDrop(itemSlot);
			}
		}

		public bool WillInteract(HandApply interaction, NetworkSide side)
		{
			if (DefaultWillInteract.Default(interaction, side) == false) return false;

			//only interaction that works is using a reagent container on this
			if (Validations.HasComponent<ReagentContainer>(interaction.HandObject) == false) return false;

			return true;
		}

		public void ServerPerformInteraction(HandApply interaction)
		{
			//Inserts reagent container
			if (itemSlot.IsOccupied)
			{
				Chat.AddExamineMsgFromServer(interaction.Performer, "The machine already has a beaker in it");
				return;
			}

			//put the reagant container inside me
			Inventory.ServerTransfer(interaction.HandSlot, itemSlot);
			UpdateGUI();
		}

		public void HeaterUpdate()
		{
			if (HeaterOn)
			{
				if (Container != null)
				{
					float Applied = 0f;
					switch (ThisState)
					{
						case PowerState.On:
							Applied = 1f;
							break;
						case PowerState.LowVoltage:
							Applied = 0.85f;
							break;
						case PowerState.OverVoltage:
							Applied = 1.25f;
							break;
						default:
							return;
					}


					if (Container.Temperature > HeaterTemperatureKelvin+1) //too hot, Removing energy
					{
						Applied *= -1;

						Applied = PowerDrawHeating * Applied;

						var Target = Container.CurrentReagentMix.WholeHeatCapacity * HeaterTemperatureKelvin;

						if (Target > Container.CurrentReagentMix.InternalEnergy + Applied) //Removed too much energy
						{
							Container.CurrentReagentMix.InternalEnergy = Target;
						}
						else
						{
							Container.CurrentReagentMix.InternalEnergy += Applied;
						}
					}
					else if (Container.Temperature < HeaterTemperatureKelvin-1) //Too cold Adding energy
					{
						Applied *= 1;

						Applied = PowerDrawHeating * Applied;

						var Target = Container.CurrentReagentMix.WholeHeatCapacity * HeaterTemperatureKelvin;

						if (Target < Container.CurrentReagentMix.InternalEnergy + Applied) //added too much energy
						{
							Container.CurrentReagentMix.InternalEnergy = Target;
						}
						else
						{
							Container.CurrentReagentMix.InternalEnergy += Applied;
						}
					}

					Container.Temperature = Container.Temperature;

					changeEvent?.Invoke();

				}
			}
		}

		public void UpdatePowerDraw()
		{
			float Total = 0.1f;
			if (HeaterOn) Total += PowerDrawHeating;
			APCPoweredDevice.Wattusage = Total;
		}


		#region IAPCPowerable

		public PowerState ThisState;

		public void PowerNetworkUpdate(float voltage) { }

		public void StateUpdate(PowerState state)
		{
			ThisState = state;
		}

		#endregion
	}
}

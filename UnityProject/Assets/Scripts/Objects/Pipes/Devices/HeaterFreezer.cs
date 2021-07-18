﻿using System;
using System.Collections.Generic;
using System.Text;
using Systems.Atmospherics;
using Systems.Electricity;
using Core.Input_System.InteractionV2.Interactions;
using Items;
using Machines;
using Messages.Server;
using Objects.Machines;
using Pipes;
using UnityEngine;

namespace Objects
{
	public class HeaterFreezer : MonoPipe, IExaminable, IRefreshParts, IInitialParts
	{
		[SerializeField]
		private float initalMinTemperature = 170;
		[SerializeField]
		private float initalMaxTemperature = 140;

		private float minTemperature;
		public float MinTemperature => minTemperature;

		private float maxTemperature;
		public float MaxTemperature => maxTemperature;

		private float currentTemperature;
		public float CurrentTemperature => currentTemperature;

		[SerializeField]
		private float targetTemperature = 298.15f;
		public float TargetTemperature => targetTemperature;

		[SerializeField]
		private float idleWattUsage = 100;

		[SerializeField]
		private HeaterFreezerType type = HeaterFreezerType.Freezer;
		public HeaterFreezerType Type => type;

		private bool isOn;
		public bool IsOn => isOn;

		private float heatCapacity = 0;

		private APCPoweredDevice apcPoweredDevice;
		public APCPoweredDevice ApcPoweredDevice => apcPoweredDevice;

		private Machine machine;

		private bool updateUi;

		public override void Awake()
		{
			base.Awake();

			apcPoweredDevice = GetComponent<APCPoweredDevice>();
			apcPoweredDevice.OnStateChangeEvent.AddListener(PowerStateChange);

			machine = GetComponent<Machine>();
		}

		private void OnDisable()
		{
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Loop);
			apcPoweredDevice.OnStateChangeEvent.RemoveListener(PowerStateChange);
		}

		public override void TickUpdate()
		{
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);

			//Only work when powered and online
			if(apcPoweredDevice.State == PowerState.Off || isOn == false) return;

			var airHeatCapacity = pipeData.mixAndVolume.WholeHeatCapacity;
			var combinedHeatCapacity = heatCapacity + airHeatCapacity;
			var oldTemperature = pipeData.mixAndVolume.Temperature;

			if (combinedHeatCapacity > 0)
			{
				var combinedEnergy = heatCapacity * targetTemperature + airHeatCapacity * oldTemperature;
				pipeData.mixAndVolume.Temperature = combinedEnergy / combinedHeatCapacity;
			}

			var temperatureDelta = Mathf.Abs(oldTemperature - pipeData.mixAndVolume.Temperature);
			if (temperatureDelta > 1)
			{
				apcPoweredDevice.Wattusage = (heatCapacity * temperatureDelta) / 10 + idleWattUsage;
			}
			else
			{
				apcPoweredDevice.Wattusage = idleWattUsage;
			}

			currentTemperature = pipeData.mixAndVolume.Temperature;
			ThreadSafeUpdateGui();
		}

		#region Examine

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			var stringBuilder = new StringBuilder();

			if (apcPoweredDevice.State == PowerState.Off)
			{
				stringBuilder.AppendLine(
					$"The {gameObject.ExpensiveName()} is unpowered.");
			}
			else
			{
				stringBuilder.AppendLine(
					$"The {gameObject.ExpensiveName()} is switched {(isOn ? "on" : "off")}.");
			}

			stringBuilder.AppendLine(
				$"The thermostat is set to {targetTemperature}K ({Celsius(targetTemperature)}C).");

			//If close enough see more detail
			if ((worldPos - registerTile.WorldPositionServer).sqrMagnitude <= 2)
			{
				stringBuilder.AppendLine($"The status display reads: Efficiency <b>{(heatCapacity / 5000 ) * 100}%.</b>");
				stringBuilder.AppendLine(
					$"Temperature range <b>{initalMinTemperature}K - {maxTemperature}K ({Celsius(initalMinTemperature)}C - {Celsius(maxTemperature)}C)</b>.");
			}

			return stringBuilder.ToString();
		}

		private string Celsius(float kelvin)
		{
			return $"{(TemperatureUtils.ZERO_CELSIUS_IN_KELVIN - kelvin) * -1}";
		}

		#endregion

		#region Machine Parts

		public void RefreshParts(IDictionary<GameObject, int> partsInFrame)
		{
			var rating = 0;

			ItemAttributesV2 partAttributes;
			foreach (var part in partsInFrame)
			{
				partAttributes = part.Key.GetComponent<ItemAttributesV2>();
				if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MatterBin))
				{
					rating += part.Key.GetComponent<StockTier>().Tier * part.Value;
				}
			}

			heatCapacity = 5000 * (Mathf.Pow((rating - 1), 2));

			if (type == HeaterFreezerType.Freezer || type == HeaterFreezerType.Both)
			{
				var minTempRating = 0;

				foreach (var part in partsInFrame)
				{
					partAttributes = part.Key.GetComponent<ItemAttributesV2>();
					if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MicroLaser))
					{
						minTempRating += part.Key.GetComponent<StockTier>().Tier * part.Value;
					}
				}

				minTemperature = Mathf.Max(TemperatureUtils.ZERO_CELSIUS_IN_KELVIN -
				                           (initalMinTemperature + minTempRating * 15), 2.7f);
				targetTemperature = minTemperature;
			}
			else
			{
				minTemperature = initalMinTemperature;
			}

			if (type == HeaterFreezerType.Heater || type == HeaterFreezerType.Both)
			{
				var maxTempRating = 0;

				foreach (var part in partsInFrame)
				{
					partAttributes = part.Key.GetComponent<ItemAttributesV2>();
					if (partAttributes.HasTrait(MachinePartsItemTraits.Instance.MicroLaser))
					{
						maxTempRating += part.Key.GetComponent<StockTier>().Tier * part.Value;
					}
				}

				maxTemperature = 293.15f + (initalMaxTemperature * maxTempRating);
				targetTemperature = maxTemperature;
			}
			else
			{
				maxTemperature = initalMaxTemperature;
			}

			UpdateGui();
		}

		public void InitialParts(IDictionary<ItemTrait, int> basicPartsUsed)
		{
			var heatCapacityRating = 0;

			foreach (var part in basicPartsUsed)
			{
				if (part.Key == MachinePartsItemTraits.Instance.MatterBin)
				{
					//Only basic matter bins so add 1 * amount there is
					heatCapacityRating += part.Value;
				}
			}

			heatCapacity = 5000 * (Mathf.Pow((heatCapacityRating - 1), 2));

			if (type == HeaterFreezerType.Freezer || type == HeaterFreezerType.Both)
			{
				var minTempRating = 0;

				foreach (var part in basicPartsUsed)
				{
					if (part.Key == MachinePartsItemTraits.Instance.MicroLaser)
					{
						//Only MicroLasers so add 1 * amount there is
						minTempRating += part.Value;
					}
				}

				minTemperature = Mathf.Max(TemperatureUtils.ZERO_CELSIUS_IN_KELVIN -
				                           (initalMinTemperature + minTempRating * 15), 2.7f);
				targetTemperature = minTemperature;
			}
			else
			{
				minTemperature = initalMinTemperature;
			}

			if (type == HeaterFreezerType.Heater || type == HeaterFreezerType.Both)
			{
				var maxTempRating = 0;

				foreach (var part in basicPartsUsed)
				{
					if (part.Key == MachinePartsItemTraits.Instance.MicroLaser)
					{
						//Only MicroLasers so add 1 * amount there is
						maxTempRating += part.Value;
					}
				}

				maxTemperature = 293.15f + (initalMaxTemperature * maxTempRating);
				targetTemperature = maxTemperature;

			}
			else
			{
				maxTemperature = initalMaxTemperature;
			}

			UpdateGui();
		}

		#endregion

		#region Interaction

		public override void HandApplyInteraction(HandApply interaction)
		{
			if (Validations.HasItemTrait(interaction.UsedObject, CommonTraits.Instance.Wrench))
			{
				//Rotate
				ToolUtils.ServerUseToolWithActionMessages(interaction, 2,
					$"You start to rotate the {gameObject.ExpensiveName()}...",
					$"{interaction.Performer.ExpensiveName()} starts to rotate the {gameObject.ExpensiveName()}...",
					$"You rotates the {gameObject.ExpensiveName()}.",
					$"{interaction.Performer.ExpensiveName()} rotates the {gameObject.ExpensiveName()}.",
					() =>
					{
						pipeData.OnDisable();

						directional.FaceDirection(directional.CurrentDirection.Rotate(1));

						SetUpPipes();
					});

				return;
			}

			if (interaction.IsAltClick)
			{
				targetTemperature = type == HeaterFreezerType.Heater ? maxTemperature : minTemperature;
				UpdateGui();
				return;
			}

			if (interaction.HandObject != null)
			{
				//Try trigger machine deconstruction interaction as it has been blocked by this script :(
				if (machine.WillInteract(interaction, NetworkSide.Server))
				{
					machine.ServerPerformInteraction(interaction);
				}

				return;
			}

			TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.ThermoMachine, TabAction.Open);
		}

		public override void AiInteraction(AiActivate interaction)
		{
			if (interaction.ClickType == AiActivate.ClickTypes.NormalClick)
			{
				TabUpdateMessage.Send(interaction.Performer, gameObject, NetTabType.ThermoMachine, TabAction.Open);
				return;
			}

			if (interaction.ClickType == AiActivate.ClickTypes.AltClick)
			{
				targetTemperature = type == HeaterFreezerType.Heater ? maxTemperature : minTemperature;
				UpdateGui();
			}
		}

		#endregion

		private void PowerStateChange(Tuple<PowerState, PowerState> states)
		{
			if (isOn == false)
			{
				ChangeSprite(false);
				return;
			}

			ChangeSprite(states.Item2 != PowerState.Off);
		}

		public void TogglePower(bool newState)
		{
			isOn = newState;

			ChangeSprite(newState);

			UpdateGui();
		}

		private void ChangeSprite(bool newState)
		{
			if (type == HeaterFreezerType.Both)
			{
				//0 is freezer off, 1 is freezer on, 2 is heater off, 3 is heater on
				spritehandler.ChangeSprite(newState ? targetTemperature > currentTemperature ? 3 : 1
					: targetTemperature > currentTemperature ? 2 : 0);
			}
			else
			{
				//0 is off, 1 is heater/freezer depending on prefab
				spritehandler.ChangeSprite(newState ? 1 : 0);
			}
		}

		public void ChangeTargetTemperature(int change)
		{
			targetTemperature += change;
			targetTemperature = Mathf.Clamp(targetTemperature, minTemperature, maxTemperature);

			if (type == HeaterFreezerType.Both && isOn)
			{
				spritehandler.ChangeSprite(targetTemperature > currentTemperature ? 3 : 1);
			}

			UpdateGui();
		}

		private void ThreadSafeUpdateGui()
		{
			//We have to update Ui this way due to thread issues with calling NetworkTabManager stuff
			if(updateUi) return;
			updateUi = true;
			UpdateManager.SafeAdd(Loop, 0.5f);
		}

		private void Loop()
		{
			UpdateGui();
			updateUi = false;
			UpdateManager.Remove(CallbackType.PERIODIC_UPDATE, Loop);
		}

		private void UpdateGui()
		{
			var peppers = NetworkTabManager.Instance.GetPeepers(gameObject, NetTabType.ThermoMachine);
			if(peppers.Count == 0) return;

			var state = ApcPoweredDevice.State == PowerState.Off ? "No Power" :
				IsOn ? "On" : "Off";

			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine($"Status: {state}");
			stringBuilder.AppendLine($"Pipe Temp: {CurrentTemperature}\n");
			stringBuilder.AppendLine($"Min Temp: {MinTemperature}");
			stringBuilder.AppendLine($"Target Temp: {TargetTemperature}");
			stringBuilder.AppendLine($"Max Temp: {MaxTemperature}");

			List<ElementValue> valuesToSend = new List<ElementValue>();
			valuesToSend.Add(new ElementValue() { Id = "TextData", Value = Encoding.UTF8.GetBytes(stringBuilder.ToString()) });
			valuesToSend.Add(new ElementValue() { Id = "SliderPower", Value = Encoding.UTF8.GetBytes((IsOn ? 1 * 100 : 0).ToString()) });

			// Update all UI currently opened.
			TabUpdateMessage.SendToPeepers(gameObject, NetTabType.ThermoMachine, TabAction.Update, valuesToSend.ToArray());
		}

		public enum HeaterFreezerType
		{
			Freezer,
			Heater,
			Both
		}
	}
}

using System;
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
		private float maxTemperature;

		[SerializeField]
		private float targetTemperature = 298.15f;

		[SerializeField]
		private float idleWattUsage = 100;

		[SerializeField]
		private HeaterFreezerType type = HeaterFreezerType.Freezer;

		private bool isOn;

		private float heatCapacity = 0;

		private APCPoweredDevice apcPoweredDevice;

		public override void Awake()
		{
			base.Awake();

			apcPoweredDevice = GetComponent<APCPoweredDevice>();
		}

		public override void OnSpawnServer(SpawnInfo info)
		{
			base.OnSpawnServer(info);

			if (type == HeaterFreezerType.Freezer)
			{
				targetTemperature = minTemperature;
			}

			if (type == HeaterFreezerType.Heater)
			{
				targetTemperature = maxTemperature;
			}
		}

		public override void TickUpdate()
		{
			//Only work when powered and online
			if(apcPoweredDevice.State == PowerState.Off || isOn == false) return;

			var gasMix = pipeData.mixAndVolume.GetGasMix();

			var airHeatCapacity = gasMix.WholeHeatCapacity;
			var combinedHeatCapacity = heatCapacity + airHeatCapacity;
			var oldTemperature = gasMix.Temperature;

			if (combinedHeatCapacity > 0)
			{
				var combinedEnergy = heatCapacity * targetTemperature + airHeatCapacity * oldTemperature;
				gasMix.SetTemperature(combinedEnergy / combinedHeatCapacity);
			}

			var temperatureDelta = Mathf.Abs(oldTemperature - gasMix.Temperature);
			if (temperatureDelta > 1)
			{
				apcPoweredDevice.Wattusage = (heatCapacity * temperatureDelta) / 10 + idleWattUsage;
			}
			else
			{
				apcPoweredDevice.Wattusage = idleWattUsage;
			}

			gasMix.SetTemperature(DMMath.Lerp(gasMix.Temperature, targetTemperature, 0.5f));
			pipeData.mixAndVolume.EqualiseWithOutputs(pipeData.Outputs);
		}

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
			}
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
			}
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
			}
		}

		private enum HeaterFreezerType
		{
			Freezer,
			Heater,
			Both
		}
	}
}

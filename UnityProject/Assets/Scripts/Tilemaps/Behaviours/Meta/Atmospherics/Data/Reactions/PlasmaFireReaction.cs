using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Systems.Atmospherics
{
	public class PlasmaFireReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var energyReleased = 0f;
			var temperature = gasMix.Temperature;
			var oldHeatCapacity = gasMix.WholeHeatCapacity;

			//More plasma released at higher temperatures
			float temperatureScale;

			if (temperature > AtmosDefines.PLASMA_UPPER_TEMPERATURE)
			{
				temperatureScale = 1;
			}
			else
			{
				//Will be decimal until PLASMA_UPPER_TEMPERATURE is reached
				temperatureScale = (temperature - AtmosDefines.PLASMA_MINIMUM_BURN_TEMPERATURE) /
				                   (AtmosDefines.PLASMA_UPPER_TEMPERATURE - AtmosDefines.PLASMA_MINIMUM_BURN_TEMPERATURE);
			}

			if (temperatureScale > 0)
			{
				//Handle plasma burning
				var oxygenMoles = gasMix.GetMoles(Gas.Oxygen);
				var plasmaMoles = gasMix.GetMoles(Gas.Plasma);

				float plasmaBurnRate;
				var oxygenBurnRate = AtmosDefines.OXYGEN_BURN_RATE_BASE - temperatureScale;

				var superSaturation = oxygenMoles / plasmaMoles > AtmosDefines.SUPER_SATURATION_THRESHOLD;

				if (oxygenMoles > plasmaMoles * AtmosDefines.PLASMA_OXYGEN_FULLBURN)
				{
					plasmaBurnRate = (plasmaMoles * temperatureScale) / AtmosDefines.PLASMA_BURN_RATE_DELTA;
				}
				else
				{
					plasmaBurnRate = (temperatureScale * (oxygenMoles / AtmosDefines.PLASMA_OXYGEN_FULLBURN)) / AtmosDefines.PLASMA_BURN_RATE_DELTA;
				}

				if (plasmaBurnRate > AtmosConstants.MINIMUM_HEAT_CAPACITY)
				{
					//Ensures matter is conserved properly
					plasmaBurnRate = Mathf.Min(plasmaBurnRate, plasmaMoles, oxygenMoles / oxygenBurnRate);

					gasMix.SetGas(Gas.Plasma, plasmaMoles - plasmaBurnRate);
					gasMix.SetGas(Gas.Oxygen, oxygenMoles - (plasmaBurnRate * oxygenBurnRate));

					if (superSaturation)
					{
						gasMix.AddGas(Gas.Tritium, plasmaBurnRate);
					}
					else
					{
						gasMix.AddGas(Gas.CarbonDioxide, plasmaBurnRate * 0.75f);
						gasMix.AddGas(Gas.WaterVapor, plasmaBurnRate * 0.25f);
					}

					energyReleased += AtmosDefines.FIRE_PLASMA_ENERGY_RELEASED * plasmaBurnRate;
				}
			}

			if (energyReleased > 0)
			{
				var newHeatCapacity = gasMix.WholeHeatCapacity;
				if (newHeatCapacity > AtmosConstants.MINIMUM_HEAT_CAPACITY)
				{
					gasMix.SetTemperature((gasMix.Temperature * oldHeatCapacity + energyReleased) / newHeatCapacity);
				}
			}

			//Create fire if possible
			if (gasMix.Temperature > AtmosDefines.FIRE_MINIMUM_TEMPERATURE_TO_EXIST)
			{
				//Dont do expose as we are off the main thread
				node.ReactionManager.ExposeHotspot(node.Position, doExposure: false);
			}
		}
	}
}

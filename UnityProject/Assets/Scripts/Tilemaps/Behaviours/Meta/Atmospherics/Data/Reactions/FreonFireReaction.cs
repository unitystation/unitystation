using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class FreonFireReaction : Reaction
	{
		private static System.Random rnd = new System.Random();

		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var energyReleased = 0f;

			var temperatureScale = 1f;

			if (gasMix.Temperature is < AtmosDefines.FREON_TRITIUM_LOWER_TEMPERATURE
			    or > AtmosDefines.FREON_MAXIMUM_BURN_TEMPERATURE)
			{
				temperatureScale = 0;
			}

			var TotalMoles = gasMix.Moles;
			var TritiumMoles = gasMix.GetMoles(Gas.Tritium);

			var HotIceCompatible = false;

			if ((TritiumMoles / TotalMoles) > 0.0001f) //0.01%
			{
				HotIceCompatible = true;
				temperatureScale = (AtmosDefines.FREON_TRITIUM_MAXIMUM_BURN_TEMPERATURE - gasMix.Temperature) /
				                   (AtmosDefines.FREON_TRITIUM_MAXIMUM_BURN_TEMPERATURE - AtmosDefines.FREON_TRITIUM_LOWER_TEMPERATURE);
			}
			else
			{
				HotIceCompatible = false;
				temperatureScale = (AtmosDefines.FREON_MAXIMUM_BURN_TEMPERATURE - gasMix.Temperature)
				                   / (AtmosDefines.FREON_MAXIMUM_BURN_TEMPERATURE - AtmosDefines.FREON_LOWER_TEMPERATURE);

				if (gasMix.Temperature is < AtmosDefines.FREON_LOWER_TEMPERATURE
				    or > AtmosDefines.FREON_MAXIMUM_BURN_TEMPERATURE)
				{
					temperatureScale = 0;
				}
			}


			var oldHeatCap = gasMix.WholeHeatCapacity;
			if (temperatureScale > 0)
			{
				var oxygenBurnRate = AtmosDefines.OXYGEN_BURN_RATE_BASE - temperatureScale;

				var freonBurnRate = 0f;

				if (gasMix.GetMoles(Gas.Oxygen) > gasMix.GetMoles(Gas.Freon) * AtmosDefines.FREON_OXYGEN_FULLBURN)
				{
					freonBurnRate = gasMix.GetMoles(Gas.Freon) * temperatureScale /
					                AtmosDefines.FREON_BURN_RATE_DELTA;
				}
				else
				{
					freonBurnRate = (temperatureScale *
					                 (gasMix.GetMoles(Gas.Oxygen) / AtmosDefines.FREON_OXYGEN_FULLBURN) /
					                 AtmosDefines.FREON_BURN_RATE_DELTA);
				}

				if (freonBurnRate > 0.0001f)
				{
					freonBurnRate = Mathf.Min(freonBurnRate, gasMix.GetMoles(Gas.Freon),
						gasMix.GetMoles(Gas.Oxygen));

					gasMix.RemoveGas(Gas.Freon, freonBurnRate);
					gasMix.RemoveGas(Gas.Oxygen, freonBurnRate * oxygenBurnRate);
					gasMix.AddGasWithTemperature(Gas.CarbonDioxide, freonBurnRate, gasMix.Temperature);

					if (gasMix.Temperature < 160 && gasMix.Temperature > 120 && rnd.Next(0, 2) == 0 && HotIceCompatible)
					{
						SpawnSafeThread.SpawnPrefab(node.WorldPosition, AtmosManager.Instance.HotIce);
					}

					energyReleased += AtmosDefines.FIRE_FREON_ENERGY_RELEASED * freonBurnRate;
				}
			}

			if (energyReleased < 0)
			{
				var newHeatCap = gasMix.WholeHeatCapacity;
				if (newHeatCap > 0.0003f)
				{
					gasMix.SetTemperature((gasMix.Temperature * oldHeatCap + energyReleased) / newHeatCap);
				}
			}
		}
	}
}
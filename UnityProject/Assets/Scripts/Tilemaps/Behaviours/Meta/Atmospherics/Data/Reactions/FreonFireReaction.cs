using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class FreonFireReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var energyReleased = 0f;
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var temperatureScale = 1f;

			if (gasMix.Temperature < AtmosDefines.FREON_LOWER_TEMPERATURE)
			{
				temperatureScale = 0;
			}
			else
			{
				temperatureScale = (AtmosDefines.FREON_MAXIMUM_BURN_TEMPERATURE - gasMix.Temperature) / (AtmosDefines.FREON_MAXIMUM_BURN_TEMPERATURE - AtmosDefines.FREON_LOWER_TEMPERATURE);
			}

			if (temperatureScale >= 0)
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
					freonBurnRate = (temperatureScale * (gasMix.GetMoles(Gas.Oxygen) / AtmosDefines.FREON_OXYGEN_FULLBURN) / AtmosDefines.FREON_BURN_RATE_DELTA);
				}

				if (freonBurnRate > 0.0001f)
				{
					freonBurnRate = Mathf.Min(freonBurnRate, gasMix.GetMoles(Gas.Freon), gasMix.GetMoles(Gas.Oxygen));

					gasMix.RemoveGas(Gas.Freon, freonBurnRate);
					gasMix.RemoveGas(Gas.Oxygen, freonBurnRate * oxygenBurnRate);

					gasMix.AddGas(Gas.CarbonDioxide, freonBurnRate);

					if (gasMix.Temperature < 160 && gasMix.Temperature > 120 && UnityEngine.Random.Range(0, 2) == 0)
					{
						Spawn.ServerPrefab(AtmosManager.Instance.hotIce, tilePos, MatrixManager.GetDefaultParent(tilePos, true));
					}

					energyReleased += AtmosDefines.FIRE_FREON_ENERGY_RELEASED * freonBurnRate;
				}
			}

			if (energyReleased < 0)
			{
				var newHeatCap = gasMix.WholeHeatCapacity;
				if (newHeatCap > 0.0003f)
				{
					gasMix.Temperature = (gasMix.Temperature * oldHeatCap + energyReleased) / newHeatCap;
				}
			}

			return 0f;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using Radiation;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Atmospherics
{
	public class TritiumFireReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var energyReleased = 0f;
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var burnedFuel = 0f;

			if (gasMix.GetMoles(Gas.Oxygen) < gasMix.GetMoles(Gas.Tritium) || AtmosDefines.MINIMUM_TRIT_OXYBURN_ENERGY > gasMix.InternalEnergy)
			{
				burnedFuel = gasMix.GetMoles(Gas.Oxygen) / AtmosDefines.TRITIUM_BURN_OXY_FACTOR;
				gasMix.RemoveGas(Gas.Tritium, burnedFuel);
			}
			else
			{
				burnedFuel = gasMix.GetMoles(Gas.Tritium) * AtmosDefines.TRITIUM_BURN_TRIT_FACTOR;
				gasMix.RemoveGas(Gas.Tritium, gasMix.GetMoles(Gas.Tritium) / AtmosDefines.TRITIUM_BURN_TRIT_FACTOR);
				gasMix.RemoveGas(Gas.Oxygen, gasMix.GetMoles(Gas.Tritium));
			}

			if (burnedFuel != 0)
			{
				energyReleased += AtmosDefines.FIRE_HYDROGEN_ENERGY_RELEASED * burnedFuel;

				if (Random.Range(0,10) == 0 && burnedFuel > AtmosDefines.TRITIUM_MINIMUM_RADIATION_ENERGY)
				{
					RadiationManager.Instance.RequestPulse(MatrixManager.AtPoint(tilePos.RoundToInt(), true).Matrix, tilePos.RoundToInt(), energyReleased / AtmosDefines.TRITIUM_BURN_RADIOACTIVITY_FACTOR, Random.Range(Int32.MinValue, Int32.MaxValue));
				}

				gasMix.AddGas(Gas.WaterVapor, burnedFuel / AtmosDefines.TRITIUM_BURN_OXY_FACTOR);
			}

			if (energyReleased > 0)
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

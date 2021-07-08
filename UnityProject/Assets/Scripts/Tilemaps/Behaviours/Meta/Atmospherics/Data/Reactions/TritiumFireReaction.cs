using System;
using System.Collections;
using System.Collections.Generic;
using Systems.Radiation;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class TritiumFireReaction : Reaction
	{
		private static System.Random rnd = new System.Random();
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, Vector3 tilePos, Matrix matrix)
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

				if (rnd.Next(0,10) == 0 && burnedFuel > AtmosDefines.TRITIUM_MINIMUM_RADIATION_ENERGY)
				{
					RadiationManager.Instance.RequestPulse(matrix, tilePos.RoundToInt(), energyReleased / AtmosDefines.TRITIUM_BURN_RADIOACTIVITY_FACTOR, rnd.Next(Int32.MinValue, Int32.MaxValue));
				}

				gasMix.AddGas(Gas.WaterVapor, burnedFuel / AtmosDefines.TRITIUM_BURN_OXY_FACTOR);
			}

			if (energyReleased > 0)
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

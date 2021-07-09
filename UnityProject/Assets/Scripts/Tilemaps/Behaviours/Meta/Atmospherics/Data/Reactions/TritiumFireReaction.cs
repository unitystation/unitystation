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

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var energyReleased = 0f;
			var oldHeatCap = gasMix.WholeHeatCapacity;
			var burnedFuel = 0f;

			if (gasMix.GetMoles(Gas.Oxygen) < gasMix.GetMoles(Gas.Tritium) || AtmosDefines.MINIMUM_TRIT_OXYBURN_ENERGY > gasMix.InternalEnergy)
			{
				burnedFuel = gasMix.GetMoles(Gas.Oxygen) / AtmosDefines.TRITIUM_BURN_OXY_FACTOR;

				gasMix.RemoveGas(Gas.Tritium, burnedFuel);
				gasMix.AddGas(Gas.WaterVapor, burnedFuel / AtmosDefines.TRITIUM_BURN_OXY_FACTOR);

				energyReleased += AtmosDefines.FIRE_HYDROGEN_ENERGY_WEAK * burnedFuel;
			}
			else
			{
				burnedFuel = gasMix.GetMoles(Gas.Tritium);

				gasMix.RemoveGas(Gas.Tritium, burnedFuel / AtmosDefines.TRITIUM_BURN_TRIT_FACTOR);
				gasMix.RemoveGas(Gas.Oxygen, burnedFuel);

				gasMix.AddGas(Gas.WaterVapor, burnedFuel / AtmosDefines.TRITIUM_BURN_TRIT_FACTOR);

				energyReleased += AtmosDefines.FIRE_HYDROGEN_ENERGY_RELEASED * burnedFuel;
			}

			if (burnedFuel != 0)
			{
				if (rnd.Next(0,10) == 0 && burnedFuel > AtmosDefines.TRITIUM_MINIMUM_RADIATION_ENERGY)
				{
					RadiationManager.Instance.RequestPulse(node.PositionMatrix, node.Position,
						energyReleased / AtmosDefines.TRITIUM_BURN_RADIOACTIVITY_FACTOR,
						rnd.Next(Int32.MinValue, Int32.MaxValue));
				}
			}

			if (energyReleased > 0)
			{
				var newHeatCap = gasMix.WholeHeatCapacity;
				if (newHeatCap > 0.0003f)
				{
					gasMix.SetTemperature((gasMix.Temperature * oldHeatCap + energyReleased) / newHeatCap);
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

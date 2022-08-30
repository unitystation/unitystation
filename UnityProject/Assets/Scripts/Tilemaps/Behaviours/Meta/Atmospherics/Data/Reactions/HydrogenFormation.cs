using UnityEngine;
using System;

namespace Systems.Atmospherics
{
	public class HydrogenFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{

			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionRate = Mathf.Min(gasMix.GetMoles(Gas.WaterVapor), gasMix.GetMoles(Gas.Plasma));

			var energyUsed = reactionRate * AtmosDefines.HYDROGEN_FORMATION_ENERGY;

			if (gasMix.GetMoles(Gas.WaterVapor) - reactionRate < 0 || gasMix.GetMoles(Gas.Plasma) - reactionRate < 0)
			{
				//No reaction
				return;
			}

			gasMix.RemoveGas(Gas.WaterVapor, reactionRate);
			gasMix.RemoveGas(Gas.Plasma, reactionRate);

			gasMix.AddGas(Gas.Hydrogen, AtmosDefines.HYDROGEN_FORMATION_RATIO * reactionRate);
			gasMix.AddGas(Gas.CarbonMonoxide, reactionRate);

			if (energyUsed > 0)
			{
				gasMix.SetTemperature(Mathf.Max((gasMix.Temperature * oldHeatCap - energyUsed) / gasMix.WholeHeatCapacity, AtmosDefines.SPACE_TEMPERATURE));
			}
		}
	}
}

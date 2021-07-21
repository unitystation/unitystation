using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class BZFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionEfficiency = Mathf.Min(
				//More efficient at less than 10 Kpa
				(float) (1 / ((gasMix.Pressure / (0.1 * 101.325))))
				* (Mathf.Max(gasMix.GetMoles(Gas.Plasma) * gasMix.GetMoles(Gas.NitrousOxide), 1f)),

				gasMix.GetMoles(Gas.NitrousOxide),
				gasMix.GetMoles(Gas.Plasma)/2);

			var energyReleased = 2 * reactionEfficiency * AtmosDefines.FIRE_CARBON_ENERGY_RELEASED;

			if (gasMix.GetMoles(Gas.NitrousOxide) - reactionEfficiency < 0 || gasMix.GetMoles(Gas.Plasma) - 2 * reactionEfficiency < 0 || energyReleased <= 0)
			{
				//No reaction
				return;
			}

			gasMix.AddGas(Gas.BZ, reactionEfficiency);

			gasMix.RemoveGas(Gas.NitrousOxide, reactionEfficiency);
			gasMix.RemoveGas(Gas.Plasma, 2 * reactionEfficiency);

			gasMix.SetTemperature(
				Mathf.Max((gasMix.Temperature * oldHeatCap + energyReleased) / gasMix.WholeHeatCapacity,
				AtmosDefines.SPACE_TEMPERATURE));
		}
	}
}
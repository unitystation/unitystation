using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class NitrylFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionEfficiency = Mathf.Min(gasMix.Temperature / 37315, gasMix.GetMoles(Gas.Oxygen), gasMix.GetMoles(Gas.Nitrogen));

			var energyUsed = reactionEfficiency * AtmosDefines.NITRYL_FORMATION_ENERGY;

			if (gasMix.GetMoles(Gas.Oxygen) - reactionEfficiency < 0 || gasMix.GetMoles(Gas.Nitrogen) - reactionEfficiency < 0 || gasMix.GetMoles(Gas.BZ) - reactionEfficiency < 0)
			{
				//No reaction
				return;
			}

			gasMix.RemoveGas(Gas.Oxygen, 2 * reactionEfficiency);
			gasMix.RemoveGas(Gas.Nitrogen, reactionEfficiency);
			gasMix.RemoveGas(Gas.BZ, 0.05f * reactionEfficiency);

			gasMix.AddGas(Gas.Nitryl, reactionEfficiency);

			if (energyUsed > 0)
			{
				gasMix.SetTemperature(
					Mathf.Max((gasMix.Temperature * oldHeatCap - energyUsed) / gasMix.WholeHeatCapacity,
					AtmosDefines.SPACE_TEMPERATURE));
			}
		}
	}
}
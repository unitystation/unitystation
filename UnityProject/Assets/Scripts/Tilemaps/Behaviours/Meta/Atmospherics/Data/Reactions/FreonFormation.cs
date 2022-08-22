using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class FreonFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionEfficiency = Mathf.Min(gasMix.Temperature / 3731.5f, gasMix.GetMoles(Gas.Plasma), gasMix.GetMoles(Gas.CarbonDioxide), gasMix.GetMoles(Gas.BZ));

			var energyUsed = reactionEfficiency * 100;

			if (gasMix.GetMoles(Gas.Plasma) - reactionEfficiency < 0 || gasMix.GetMoles(Gas.CarbonDioxide) - reactionEfficiency < 0 || gasMix.GetMoles(Gas.BZ) - reactionEfficiency < 0)
			{
				//No reaction
				return;
			}

			gasMix.RemoveGas(Gas.Plasma, 5 * reactionEfficiency);
			gasMix.RemoveGas(Gas.CarbonDioxide, reactionEfficiency);
			gasMix.RemoveGas(Gas.BZ, 0.25f * reactionEfficiency);

			gasMix.AddGas(Gas.Freon, reactionEfficiency * 2);

			if (energyUsed > 0)
			{
				gasMix.SetTemperature(
					Mathf.Max((gasMix.Temperature * oldHeatCap - energyUsed) / gasMix.WholeHeatCapacity,
					AtmosDefines.SPACE_TEMPERATURE));
			}
		}
	}
}

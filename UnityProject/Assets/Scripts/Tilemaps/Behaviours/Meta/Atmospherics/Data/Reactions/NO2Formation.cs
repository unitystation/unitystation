using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class NO2Formation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionEfficiency = Mathf.Min(gasMix.GetMoles(Gas.Oxygen), gasMix.GetMoles(Gas.Nitrogen));

			var energyUsed = reactionEfficiency * AtmosDefines.NITROUS_FORMATION_ENERGY;

			if (gasMix.GetMoles(Gas.Oxygen) - reactionEfficiency < 0 || gasMix.GetMoles(Gas.Nitrogen) - reactionEfficiency < 0)
			{
				//No reaction
				return 0f;
			}

			if (gasMix.Temperature > 250f)
			{
				//No reaction
				return 0f;
			}

			gasMix.RemoveGas(Gas.Oxygen, reactionEfficiency);
			gasMix.RemoveGas(Gas.Nitrogen, 2 * reactionEfficiency);

			gasMix.AddGas(Gas.NitrousOxide, reactionEfficiency);

			if (energyUsed > 0)
			{
				gasMix.Temperature = Mathf.Max((gasMix.Temperature * oldHeatCap - energyUsed)/gasMix.WholeHeatCapacity, 2.7f);
			}

			return 0f;
		}
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class HyperNobliumFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var reactionEfficiency = Mathf.Min(gasMix.GetMoles(Gas.Nitrogen) + gasMix.GetMoles(Gas.Tritium) / 100, gasMix.GetMoles(Gas.Tritium) / 10, gasMix.GetMoles(Gas.Nitrogen) / 20);

			var energyUsed = reactionEfficiency * (AtmosDefines.NOBLIUM_FORMATION_ENERGY / Mathf.Max(gasMix.GetMoles(Gas.BZ), 1f));

			if (gasMix.GetMoles(Gas.Tritium) - 10 * reactionEfficiency < 0 || gasMix.GetMoles(Gas.Nitrogen) - 20 * reactionEfficiency < 0)
			{
				//No reaction
				return 0f;
			}

			gasMix.RemoveGas(Gas.Tritium, 10 * reactionEfficiency);
			gasMix.RemoveGas(Gas.Nitrogen, 20 * reactionEfficiency);

			gasMix.AddGas(Gas.HyperNoblium, reactionEfficiency);

			gasMix.Temperature = Mathf.Max((gasMix.Temperature * oldHeatCap - energyUsed)/gasMix.WholeHeatCapacity, 2.7f);

			return 0f;
		}
	}
}
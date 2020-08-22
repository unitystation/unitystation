using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class StimulumFormation : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var heatScale = Mathf.Min(gasMix.Temperature/AtmosDefines.STIMULUM_HEAT_SCALE, gasMix.GetMoles(Gas.Tritium), gasMix.GetMoles(Gas.Plasma), gasMix.GetMoles(Gas.Nitryl));

			var stimEnergyChange = heatScale + AtmosDefines.STIMULUM_FIRST_RISE * Mathf.Pow(heatScale, 2) -
			                       AtmosDefines.STIMULUM_FIRST_DROP * Mathf.Pow(heatScale, 3) +
			                       AtmosDefines.STIMULUM_SECOND_RISE * Mathf.Pow(heatScale, 4) -
			                       AtmosDefines.STIMULUM_ABSOLUTE_DROP * Mathf.Pow(heatScale, 5);

			if (gasMix.GetMoles(Gas.Tritium) - heatScale < 0 || gasMix.GetMoles(Gas.Plasma) - heatScale < 0 || gasMix.GetMoles(Gas.Nitryl) - heatScale < 0)
			{
				//No reaction
				return 0f;
			}

			gasMix.AddGas(Gas.Stimulum, heatScale / 10f);

			gasMix.RemoveGas(Gas.Tritium, heatScale);
			gasMix.RemoveGas(Gas.Plasma, heatScale);
			gasMix.RemoveGas(Gas.Nitryl,  heatScale);

			gasMix.Temperature = Mathf.Max((gasMix.Temperature * oldHeatCap + stimEnergyChange)/gasMix.WholeHeatCapacity, 2.7f);

			return 0f;
		}
	}
}
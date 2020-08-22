using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class NO2Decomposition : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			var oldHeatCap = gasMix.WholeHeatCapacity;

			var burnedFuel = 0f;

			burnedFuel = Mathf.Max(0f, 0.00002f * (gasMix.Temperature - (0.00001f * Mathf.Pow(gasMix.Temperature, 2)))) * gasMix.GetMoles(Gas.NitrousOxide);
			gasMix.RemoveGas(Gas.NitrousOxide, burnedFuel);

			if (burnedFuel != 0)
			{
				var energyReleased = AtmosDefines.N2O_DECOMPOSITION_ENERGY_RELEASED * burnedFuel;

				gasMix.AddGas(Gas.Oxygen, burnedFuel / 2f);
				gasMix.AddGas(Gas.Nitrogen, burnedFuel);

				var newHeatCap = gasMix.WholeHeatCapacity;
				if (newHeatCap > 0.0003f)
				{
					gasMix.Temperature = (gasMix.Temperature + energyReleased) / newHeatCap;
				}
			}

			return 0f;
		}
	}
}

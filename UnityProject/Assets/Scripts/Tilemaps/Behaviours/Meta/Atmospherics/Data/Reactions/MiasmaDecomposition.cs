using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class MiasmaDecomposition : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			if (gasMix.GetMoles(Gas.WaterVapor) != 0 && gasMix.GetMoles(Gas.WaterVapor) / gasMix.Moles > 0.1)
			{
				//No reaction
				return;
			}

			var cleanedAir = Mathf.Min(gasMix.GetMoles(Gas.Miasma), 20 + (gasMix.Temperature - 373.15f - 70) / 20);

			gasMix.RemoveGas(Gas.Miasma, cleanedAir);

			gasMix.AddGas(Gas.Oxygen, cleanedAir);

			gasMix.SetTemperature(gasMix.Temperature + cleanedAir * 0.002f);
		}
	}
}

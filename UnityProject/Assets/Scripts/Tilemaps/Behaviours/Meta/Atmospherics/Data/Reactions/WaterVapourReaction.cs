using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Atmospherics
{
	public class WaterVapourReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public float React(ref GasMix gasMix)
		{
			if (gasMix.Temperature <= AtmosDefines.WATER_VAPOR_FREEZE)
			{
				//ToDo spawn ice times amount of moles, also add script to ice to melt back into water vapour

				gasMix.SetGas(Gas.WaterVapor, 0f);
			}

			return 0f;
		}
	}
}

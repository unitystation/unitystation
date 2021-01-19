using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Systems.Atmospherics
{
	public class WaterVapourReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, Vector3 tilePos, Matrix matrix)
		{
			if (gasMix.Temperature <= AtmosDefines.WATER_VAPOR_FREEZE)
			{
				if (gasMix.GetMoles(Gas.WaterVapor) < 2f)
				{
					//Not enough moles to freeze
					return;
				}

				var numberOfIceToSpawn = Mathf.Floor(gasMix.GetMoles(Gas.WaterVapor) / 2f);

				for (var i = 0; i < numberOfIceToSpawn; i++)
				{
					SpawnSafeThread.SpawnPrefab(tilePos, AtmosManager.Instance.iceShard);
				}

				gasMix.RemoveGas(Gas.WaterVapor, numberOfIceToSpawn * 2f);
			}
		}
	}
}

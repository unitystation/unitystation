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

		public float React(ref GasMix gasMix, Vector3 tilePos)
		{
			if (gasMix.Temperature <= AtmosDefines.WATER_VAPOR_FREEZE)
			{
				if (gasMix.GetMoles(Gas.WaterVapor) < 2f)
				{
					//Not enough moles to freeze
					return 0f;
				}

				var numberOfIceToSpawn = Mathf.Floor(gasMix.GetMoles(Gas.WaterVapor) / 2f);

				for (var i = 0; i < numberOfIceToSpawn; i++)
				{
					Spawn.ServerPrefab(AtmosManager.Instance.iceShard, tilePos, MatrixManager.GetDefaultParent(tilePos, true));
				}

				gasMix.RemoveGas(Gas.WaterVapor, numberOfIceToSpawn * 2f);
			}

			return 0f;
		}
	}
}

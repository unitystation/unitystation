using UnityEngine;
using US13.Core.Lifecycle;

namespace US13.Tilemaps.Behaviours.Meta.Atmospherics.Data.Reactions
{
	public class WaterVapourReaction : Reaction
	{
		public bool Satisfies(GasMix gasMix)
		{
			throw new System.NotImplementedException();
		}

		public void React(GasMix gasMix, MetaDataNode node)
		{
			if (gasMix.Temperature > AtmosDefines.WATER_VAPOR_FREEZE) return;

			if (gasMix.GetMoles(Gas.WaterVapor) < 2f)
			{
				//Not enough moles to freeze
				return;
			}

			var numberOfIceToSpawn = (int)Mathf.Floor(gasMix.GetMoles(Gas.WaterVapor) / 2f);

			//Stack size of ice is 50
			if (numberOfIceToSpawn > 50)
			{
				numberOfIceToSpawn = 50;
			}

			if (numberOfIceToSpawn < 1) return;

			SpawnSafeThread.SpawnPrefab(node.WorldPosition, AtmosManager.Instance.IceShard, amountIfStackable: numberOfIceToSpawn);

			gasMix.RemoveGas(Gas.WaterVapor, numberOfIceToSpawn * 2f);
		}
	}
}

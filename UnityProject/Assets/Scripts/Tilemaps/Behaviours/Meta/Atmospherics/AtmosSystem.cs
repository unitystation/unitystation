using UnityEngine;

namespace Systems.Atmospherics
{
	public class AtmosSystem : SubsystemBehaviour
	{
		public override SystemType SubsystemType => SystemType.AtmosSystem;

		public override void Initialize()
		{
			if (!CustomNetworkManager.IsServer)
				return;

			BoundsInt bounds = metaTileMap.GetBounds();

			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				//get toptile at pos to check if it should spawn with no air
				bool spawnWithNoAir = false;
				var topTile = metaTileMap.GetTile(position, LayerTypeSelection.Effects | LayerTypeSelection.Underfloor);
				if (topTile is BasicTile tile)
				{
					spawnWithNoAir = tile.SpawnWithNoAir;
				}
				MetaDataNode node = metaDataLayer.Get(position, false);
				if ((node.IsRoom || node.IsOccupied) && !spawnWithNoAir)
				{
					node.GasMix = GasMix.NewGasMix(GasMixes.BaseAirMix);
				}
				else
				{
					node.GasMix = GasMix.NewGasMix(GasMixes.BaseSpaceMix);
				}
			}
		}

		public override void UpdateAt(Vector3Int localPosition)
		{
			AtmosThread.Enqueue(metaDataLayer.Get(localPosition));
		}
	}
}

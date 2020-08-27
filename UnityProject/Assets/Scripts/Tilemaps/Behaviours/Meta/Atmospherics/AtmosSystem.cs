using System.Diagnostics;
using System.Threading;
using Atmospherics;
using UnityEngine;

public class AtmosSystem : SubsystemBehaviour
{
	public override void Initialize()
	{
		BoundsInt bounds = metaTileMap.GetBounds();

		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			//get toptile at pos to check if it should spawn with no air
			bool spawnWithNoAir = false;
			var topTile = metaTileMap.GetTile(position,LayerTypeSelection.Effects | LayerTypeSelection.Underfloor);
			if (topTile is BasicTile tile)
			{
				spawnWithNoAir = tile.SpawnWithNoAir;
			}
			MetaDataNode node = metaDataLayer.Get(position, false);
			node.GasMix = new GasMix( (node.IsRoom||node.IsOccupied) && !spawnWithNoAir ? GasMixes.Air : GasMixes.Space );
		}
	}

	public override void UpdateAt(Vector3Int localPosition)
	{
		AtmosThread.Enqueue(metaDataLayer.Get(localPosition));
	}
}
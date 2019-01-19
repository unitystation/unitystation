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
			MetaDataNode node = metaDataLayer.Get(position, false);

			node.Atmos = node.IsRoom ? GasMixes.Air : GasMixes.Space;
		}
	}

	public override void UpdateAt(Vector3Int position)
	{
		AtmosThread.Enqueue(metaDataLayer.Get(position));
	}
}
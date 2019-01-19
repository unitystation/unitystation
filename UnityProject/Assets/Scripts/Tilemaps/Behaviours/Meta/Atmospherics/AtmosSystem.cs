using System.Threading;
using Atmospherics;
using UnityEngine;

public class AtmosSystem : SubsystemBehaviour
{
	public float Speed = 0.1f;

	public override void Initialize()
	{
		InitializeAtmos();
	}

	public override void UpdateAt(Vector3Int position)
	{
		AtmosThread.Enqueue(metaDataLayer.Get(position));
	}

	private void OnValidate()
	{
		AtmosThread.SetSpeed(Speed);
	}

	private void OnDestroy()
	{
		AtmosThread.Stop();
	}

	private void InitializeAtmos()
	{
		BoundsInt bounds = metaTileMap.GetBounds();

		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			MetaDataNode node = metaDataLayer.Get(position, false);

			node.Atmos = node.IsRoom ? GasMixes.Air : GasMixes.Space;
		}
	}
}
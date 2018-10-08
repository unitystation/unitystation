using System.Threading;
using Atmospherics;
using UnityEngine;

public class AtmosSystem : SubsystemBehaviour
{
	public float Speed = 0.1f;

	private AtmosThread thread;

	public override void Initialize()
	{
		InitializeAtmos();

		thread = new AtmosThread(metaDataLayer);
		new Thread(thread.Run).Start();
	}

	public override void UpdateAt(Vector3Int position)
	{
		thread?.Enqueue(position);
	}

	public int GetUpdateListCount()
	{
		return thread?.GetUpdateListCount() ?? 0;
	}

	private void OnValidate()
	{
		thread?.SetSpeed(Speed);
	}

	private void OnDestroy()
	{
		thread?.Stop();
	}

	private void InitializeAtmos()
	{
		BoundsInt bounds = metaTileMap.GetBounds();

		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			MetaDataNode node = metaDataLayer.Get(position, false);

			if (node.IsRoom)
			{
				AtmosUtils.SetAir(node);
			}
			else
			{
				AtmosUtils.SetEmpty(node);
			}
		}
	}
}
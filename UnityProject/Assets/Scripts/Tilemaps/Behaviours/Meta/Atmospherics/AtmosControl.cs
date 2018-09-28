using System.Threading;
using Atmospherics;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;

public class AtmosControl : SystemBehaviour
{
	private AtmosThread thread;

	public override void Initialize()
	{
		thread = new AtmosThread(metaDataLayer);
		new Thread(thread.Run).Start();

		InitializeAtmos();
	}

	public override void UpdateAt(Vector3Int position)
	{
		thread?.Enqueue(position);
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

			switch (node.Type)
			{
				case NodeType.Room:
					AtmosUtils.SetAir(node);
					break;
				case NodeType.Space:
					AtmosUtils.SetEmpty(node);
					break;
			}
		}
	}
}
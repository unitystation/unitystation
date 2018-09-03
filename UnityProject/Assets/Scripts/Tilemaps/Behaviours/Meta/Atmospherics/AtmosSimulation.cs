using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

namespace Atmospherics
{
	public class AtmosSimulation
	{
		private readonly MetaDataLayer metaDataLayer;

		private UniqueQueue<MetaDataNode> updateList = new UniqueQueue<MetaDataNode>();

		public bool IsIdle => updateList.IsEmpty;

		public AtmosSimulation(MetaDataLayer metaDataLayer)
		{
			this.metaDataLayer = metaDataLayer;
		}

		public void AddToUpdateList(Vector3Int position)
		{
			AddToUpdateList(metaDataLayer.Get(position));
		}

		private void AddToUpdateList(MetaDataNode node)
		{
			updateList.Enqueue(node);
		}

		public void Run()
		{
			MetaDataNode node;

			while (updateList.TryDequeue(out node))
			{
				Update(node);
			}
		}

		private void Update(MetaDataNode node)
		{
			var nodes = new List<MetaDataNode>(5) {node};
			MetaDataNode[] neighbors = node.Neighbors.ToArray();

			nodes.AddRange(neighbors);

			if (node.IsOccupied || IsPressureChanged(node))
			{
				AtmosUtils.Equalize(nodes);

				updateList.EnqueueAll(neighbors);
			}
		}

		private static bool IsPressureChanged(MetaDataNode node)
		{
			foreach (MetaDataNode neighbor in node.Neighbors)
			{
				if (Mathf.Abs(node.Atmos.Pressure - neighbor.Atmos.Pressure) > AtmosUtils.MinimumPressure)
				{
					return true;
				}
			}

			return false;
		}
	}
}
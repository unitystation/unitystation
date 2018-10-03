using System.Collections.Generic;
using Tilemaps.Behaviours.Meta;
using Tilemaps.Behaviours.Meta.Utils;
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
			updateList.Enqueue(metaDataLayer.Get(position));
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

			MetaDataNode[] neighbors = node.GetNeighbors();

			nodes.AddRange(neighbors);

			if (node.IsOccupied || AtmosUtils.IsPressureChanged(node))
			{
				Equalize(nodes);

				updateList.EnqueueAll(neighbors);
			}
		}

		private static void Equalize(IReadOnlyCollection<MetaDataNode> nodes)
		{
			List<MetaDataNode> targetNodes = new List<MetaDataNode>();
			GasMix gasMix = GasMixUtils.Space;

			foreach (MetaDataNode node in nodes)
			{
				gasMix += node.Atmos;

				switch (node.Type)
				{
					case NodeType.Room:
						targetNodes.Add(node);
						break;
					case NodeType.Occupied:
						AtmosUtils.SetEmpty(node);
						break;
					case NodeType.Space:
						targetNodes.Add(node);
						AtmosUtils.SetEmpty(nodes);
						return; // exit here
				}
			}

			gasMix /= targetNodes.Count;

			// Copy to other nodes
			for (int i = 0; i < targetNodes.Count; i++)
			{
				targetNodes[i].Atmos = gasMix;
			}
		}
	}
}
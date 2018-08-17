using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Meta.Data;
using Tilemaps.Utils;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.SceneManagement;

namespace Tilemaps.Behaviours.Meta
{
	public class Atmospherics
	{
		private const float MinimumPressure = 0.0001f;

		private readonly MetaDataLayer metaDataLayer;

		private UniqueQueue<MetaDataNode> updateList = new UniqueQueue<MetaDataNode>();

		public bool IsIdle => updateList.IsEmpty;

		public Atmospherics(MetaDataLayer metaDataLayer)
		{
			this.metaDataLayer = metaDataLayer;
		}

		public void AddToUpdateList(Vector3Int position)
		{
			updateList.Enqueue(metaDataLayer.Get(position));
		}

		public void Run()
		{
			int count = updateList.Count;
			for (int i = 0; i < count; i++)
			{
				MetaDataNode node;

				if (!updateList.TryDequeue(out node))
				{
					break;
				}

				Update(node);
			}
		}

		private void Update(MetaDataNode node)
		{
			var nodes = new List<MetaDataNode> {node};
			nodes.AddRange(node.Neighbors);

			if (node.IsOccupied || IsPressureChanged(node))
			{
				AtmosUtils.Equalize(nodes);

				updateList.EnqueueAll(node.Neighbors);
			}
		}

		private static bool IsPressureChanged(MetaDataNode node)
		{
			foreach (MetaDataNode neighbor in node.Neighbors)
			{
				if (Mathf.Abs(node.Atmos.Pressure - neighbor.Atmos.Pressure) > MinimumPressure)
				{
					return true;
				}
			}

			return false;
		}
	}
}
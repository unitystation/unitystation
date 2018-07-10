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
using UnityEngine.SceneManagement;

namespace Tilemaps.Behaviours.Meta
{
	public class Atmospherics
	{
		private const float MinimumPressure = 0.0001f;

		private static Vector3Int[] offsets = {Vector3Int.zero, Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left};

		private readonly MetaDataLayer metaDataLayer;

		private UniqueQueue<Vector3Int> updateList = new UniqueQueue<Vector3Int>();
		private UniqueQueue<Vector3Int> edgeList = new UniqueQueue<Vector3Int>();

		public bool IsIdle => updateList.IsEmpty && edgeList.Count == 0;

		public Atmospherics(MetaDataLayer metaDataLayer)
		{
			this.metaDataLayer = metaDataLayer;
		}

		public void AddToUpdateList(Vector3Int position)
		{
			updateList.Enqueue(position);
		}

		public void run()
		{
			Equalize();
			CalculateEdge();
		}

		private void Equalize()
		{
			int count = updateList.Count;
			for (int i = 0; i < count; i++)
			{
				Vector3Int position;
				if (!updateList.TryDequeue(out position))
					break;

				if (Equalize(position))
				{
					updateList.Enqueue(position);
				}
				else
				{
					edgeList.Enqueue(position);
				}
			}
		}

		private bool Equalize(Vector3Int position)
		{
			var mix = new float[Gas.Count];
			var jm = new float[Gas.Count];
			var nodes = new List<MetaDataNode>(5);

			bool active = false;
			bool isSpace = false;

			MetaDataNode mainNode = metaDataLayer.Get(position);

			foreach (Vector3Int offset in offsets)
			{
				Vector3Int newPosition = offset + position;

				MetaDataNode node = metaDataLayer.Get(newPosition, false);

				if (node.IsSpace)
				{
					isSpace = true;
					break;
				}

				for (int i = 0; i < Gas.Count; i++)
				{
					mix[i] += node.Atmos.AirMix[i];
					jm[i] += node.Atmos.AirMix[i] * Gas.Get(i).HeatCapacity * node.Atmos.Temperature;
				}

				if (node.IsRoom)
				{
					if (Mathf.Abs(mainNode.Atmos.Pressure - node.Atmos.Pressure) > MinimumPressure)
					{
						active = true;
					}

					nodes.Add(node);

					if (mainNode != node && !updateList.Contains(newPosition))
					{
						AddToEdgeList(newPosition, node);
					}
				}
				else
				{
					AtmosUtils.SetEmpty(node);
				}
			}

			if (isSpace)
			{
				foreach (Vector3Int offset in offsets)
				{
					Vector3Int newPosition = offset + position;

					MetaDataNode node = metaDataLayer.Get(newPosition, false);

					if (node.IsRoom)
					{
						AtmosUtils.SetEmpty(node);

						if (mainNode != node && !updateList.Contains(newPosition))
						{
							AddToEdgeList(newPosition, node);
						}
					}
				}
			}
			else
			{
				AtmosUtils.SetAirMixes(mix, jm, nodes);
			}

			mainNode.Atmos.State = active ? AtmosState.Updating : AtmosState.Edge;

			return mainNode.IsRoom && active;
		}

		private void CalculateEdge()
		{
			int count = edgeList.Count;
			for (int i = 0; i < count; i++)
			{
				Vector3Int position;
				if (!edgeList.TryDequeue(out position))
					break;

				MetaDataNode node = metaDataLayer.Get(position);
				node.Atmos.State = AtmosState.None;

				if (!node.IsRoom)
				{
					continue;
				}

				foreach (Vector3Int offset in offsets)
				{
					Vector3Int newPosition = position + offset;
					MetaDataNode neighborNode = metaDataLayer.Get(newPosition);

					if (neighborNode.IsRoom)
					{
						if (Mathf.Abs(neighborNode.Atmos.Pressure - node.Atmos.Pressure) > MinimumPressure)
						{
							AddToEdgeList(newPosition, neighborNode);

							if (!updateList.Contains(position))
							{
								AddToUpdateList(position, node);
							}
						}
					}
				}
			}
		}

		private void AddToEdgeList(Vector3Int newPosition, MetaDataNode neighborNode)
		{
			edgeList.Enqueue(newPosition);
			neighborNode.Atmos.State = AtmosState.Edge;
		}

		private void AddToUpdateList(Vector3Int position, MetaDataNode node)
		{
			updateList.Enqueue(position);
			node.Atmos.State = AtmosState.Updating;
		}
	}
}
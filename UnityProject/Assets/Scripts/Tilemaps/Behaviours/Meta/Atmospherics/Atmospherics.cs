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
		private static Vector3Int[] offsets = {Vector3Int.zero, Vector3Int.up, Vector3Int.right, Vector3Int.down, Vector3Int.left};

		private MetaDataLayer metaDataLayer;
		private ConcurrentQueue<Vector3Int> updateList = new ConcurrentQueue<Vector3Int>();
		private HashSet<Vector3Int> edgeList = new HashSet<Vector3Int>();

		public bool IsIdle => updateList.IsEmpty && edgeList.Count == 0;

		public Atmospherics(MetaDataLayer metaDataLayer)
		{
			this.metaDataLayer = metaDataLayer;
		}

		public void AddToUpdateList(Vector3Int position)
		{
			updateList.Enqueue(position);
			metaDataLayer.Get(position).updating = true;
		}

		public void run()
		{
			MoveGas();
			CalculateEdge();
		}

		private List<List<Vector3Int>> areasToUpdate = new List<List<Vector3Int>>();

		public class Area
		{
			private HashSet<Vector3Int> area = new HashSet<Vector3Int>();
			private HashSet<Vector3Int> border = new HashSet<Vector3Int>();

			public Area(Vector3Int start)
			{
				area.Add(start);
				border.Add(start);
			}
			
			public void Expand()
			{
				
			}

			public void Equalize()
			{
				
			}

			public void Remove(Vector3Int position)
			{
				area.Remove(position);
				border.Remove(position);
			}
		}

		private void MoveGas2()
		{
			foreach (List<Vector3Int> area in areasToUpdate)
			{
				// expand
				// 	 if it couldn't be expanded
				// equalize
			}
		}

		private void Expand(List<Vector3Int> area)
		{
			// Find border TODO save border
		}

		private void Equalize(List<Vector3Int> area)
		{
			
		}

		private void MoveGas()
		{
			int count = updateList.Count;
			for (int i = 0; i < count; i++)
			{
				Vector3Int position;
				if (!updateList.TryDequeue(out position))
					break;

				if (MoveGas(position))
				{
					edgeList.Add(position);
					metaDataLayer.Get(position).updating = false;
					metaDataLayer.Get(position).atmosEdge = true;
				}
				else
				{
					updateList.Enqueue(position);
					metaDataLayer.Get(position).updating = true;
					metaDataLayer.Get(position).atmosEdge = false;
				}
			}
		}

		private bool MoveGas(Vector3Int position)
		{
			var mix = new float[Gas.Count];
			var jm = new float[Gas.Count];
			var nodes = new List<MetaDataNode>();

			bool decay = true;
			bool isSpace = false;

			MetaDataNode mainNode = metaDataLayer.Get(position);

			if (!mainNode.IsRoom)
			{
				return true;
			}

			foreach (Vector3Int offset in offsets)
			{
				MetaDataNode node = metaDataLayer.Get(offset + position, false);

				// TODO might add an IsPassable-Check with direction

				if (node.IsSpace)
				{
					isSpace = true;
					break;
				}

				for (int i = 0; i < Gas.Count; i++)
				{
					mix[i] += node.AirMix[i];
					jm[i] += node.AirMix[i] * Gas.Get(i).HeatCapacity * node.Temperature;
				}

				if (node.IsRoom)
				{
					if (Mathf.Abs(mainNode.Pressure - node.Pressure) > 0.01f)
					{
						decay = false;
					}

					nodes.Add(node);

					if (mainNode != node && !updateList.Contains(offset + position))
					{
						edgeList.Add(offset + position);
						metaDataLayer.Get(offset + position).atmosEdge = true;
					}
				}
				else
				{ // Actually not space....
					SetSpace(node);
				}
			}

			if (isSpace)
			{
				foreach (Vector3Int offset in offsets)
				{
					MetaDataNode node = metaDataLayer.Get(offset + position, false);
					SetSpace(node);
				}
			}
			else
			{
				SetAirMixes(mix, jm, nodes);
			}
			
			

			return decay;
		}

		private static void SetSpace(MetaDataNode node)
		{
			node.AirMix = new float[Gas.Count];
			node.Temperature = 2.7f;
			node.Moles = 0.000000000000281f;
		}

		private static void SetAirMixes(float[] mix, float[] jm, List<MetaDataNode> nodes)
		{
			var moles = 0.0f;
			var temperature = 0.0f;

			var count = 0;

			// Calculate new values for first node
			for (int i = 0; i < Gas.Count; i++)
			{
				if (mix[i] > 0)
				{
					temperature += jm[i] / (Gas.Get(i).HeatCapacity * mix[i]);
					mix[i] /= nodes.Count;
					moles += mix[i];
					count++;
				}
			}

			temperature /= count;

			// Copy to other nodes
			for (int i = 0; i < nodes.Count; i++)
			{
				// TODO own airMix array which is just referenced and not copied ?
				Array.Copy(mix, nodes[i].AirMix, Gas.Count);
				nodes[i].Temperature = temperature;
				nodes[i].Moles = moles;
			}
		}

		public void CalculateEdge()
		{
			var newEdges = new HashSet<Vector3Int>();

			foreach (Vector3Int position in edgeList)
			{
				metaDataLayer.Get(position).atmosEdge = false;
				bool decay = true;
				MetaDataNode node = metaDataLayer.Get(position);

				if (!node.IsRoom)
				{
					continue;
				}

				foreach (Vector3Int offset in offsets)
				{
					Vector3Int pos = position + offset;
					MetaDataNode neighbor = metaDataLayer.Get(pos);

					if (neighbor.IsRoom)
					{
						if (Mathf.Abs(neighbor.Pressure - node.Pressure) > 0.01f)
						{
							decay = false;
							newEdges.Add(pos);
							metaDataLayer.Get(pos).atmosEdge = true;
							metaDataLayer.Get(position).updating = true;
							
							if(!updateList.Contains(position))
								updateList.Enqueue(position);
						}
					}
				}
			}

			edgeList = newEdges;
		}
	}
}
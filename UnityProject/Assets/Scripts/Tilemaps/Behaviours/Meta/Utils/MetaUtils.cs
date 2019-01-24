using System.Collections.Generic;
using UnityEngine;

namespace Tilemaps.Behaviours.Meta
{
	public static class MetaUtils
	{
		private static Vector3Int[] directions = {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right};


		public static Vector3Int[] GetNeighbors(Vector3Int center)
		{
			var neighbors = new Vector3Int[directions.Length];

			for (int i = 0; i < directions.Length; i++)
			{
				neighbors[i] = center + directions[i];
			}

			return neighbors;
		}

		public static void AddToNeighbors(MetaDataNode node)
		{
			for (var i = 0; i < node.Neighbors.Count; i++)
			{
				node.Neighbors[i].AddNeighbor(node);
			}
		}

		public static void RemoveFromNeighbors(MetaDataNode node)
		{
			for (var i = 0; i < node.Neighbors.Count; i++)
			{
				node.Neighbors[i].RemoveNeighbor(node);
			}
		}
	}
}
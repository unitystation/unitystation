using System.Collections.Generic;
using UnityEngine;

namespace Tilemaps.Behaviours.Meta
{
	public static class MetaUtils
	{
		public static readonly Vector3Int[] Directions = {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right};


		public static Vector3Int[] GetNeighbors(Vector3Int center, Vector3Int[] dir)
		{
			if(dir == null)
			{
				dir = Directions;
			}
			var neighbors = new Vector3Int[dir.Length];

			for (int i = 0; i < dir.Length; i++)
			{
				neighbors[i] = center + dir[i];
			}

			return neighbors;
		}

		public static void AddToNeighbors(MetaDataNode node)
		{
			for (var i = 0; i < node.Neighbors.Length; i++)
			{
				node.Neighbors[i]?.AddNeighbor(node);
			}
		}

		public static void RemoveFromNeighbors(MetaDataNode node)
		{
			for (var i = 0; i < node.Neighbors.Length; i++)
			{
				node.Neighbors[i]?.RemoveNeighbor(node);
			}
		}
	}
}
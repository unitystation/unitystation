using System.Collections.Generic;
using UnityEngine;

namespace Tilemaps.Behaviours.Meta
{
	public static class MetaUtils
	{
		private static Vector3Int[] directions = {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right};


		public static IEnumerable<Vector3Int> GetNeighbors(Vector3Int center)
		{
			var neighbors = new Vector3Int[directions.Length];

			for (var i = 0; i < directions.Length; i++)
			{
				neighbors[i] = center + directions[i];
			}

			return neighbors;
		}

		public static void AddToNeighbors(MetaDataNode node)
		{
			foreach (MetaDataNode neighbor in node.Neighbors)
			{
				neighbor.AddNeighbor(node);
			}
		}

		public static void RemoveFromNeighbors(MetaDataNode node)
		{
			foreach (MetaDataNode neighbor in node.Neighbors)
			{
				neighbor.RemoveNeighbor(node);
			}
		}
		
	}
}
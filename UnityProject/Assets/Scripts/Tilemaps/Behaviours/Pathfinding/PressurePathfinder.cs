using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace Tilemaps.Behaviours.Pathfinding
{
	public class PressurePathfinder
	{
		/// <summary>
		///     Returns whether or not the given position is out of bounds of the pathfinding array.
		/// </summary>
		/// <param name="terrain">the tilemap/grid used</param>
		/// <param name="gridPosition">The location to check.</param>
		private bool PositionIsOutOfBounds(ChunkedTileMap<MetaDataNode> terrain, Vector3Int gridPosition)
		{
			if (gridPosition.x < 0 || gridPosition.y < 0 ||
			    gridPosition.x >= terrain.MaxX || gridPosition.y >= terrain.MaxY) return true;
			return false;
		}

		/// <summary>
		/// Gets the most efficent path from point A to B.
		/// </summary>
		/// <param name="terrain">the tilemap that will be used to treverse</param>
		/// <param name="start">starting position of the path</param>
		/// <param name="end">end of path</param>
		/// <returns>a list of vector3s that create a path from `start` to `end`</returns>
		public List<Vector3Int> FromTo(ChunkedTileMap<MetaDataNode> terrain, Vector3Int start, Vector3Int end)
        {
            if (PositionIsOutOfBounds(terrain, start) || PositionIsOutOfBounds(terrain, end))
                return null;

            var queue = new Queue<Vector3Int>();
            var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            var path = new List<Vector3Int>();

            queue.Enqueue(start);
            cameFrom[start] = start;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == end)
                {
                    while (current != start)
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Add(start);
                    path.Reverse();
                    return path;
                }

                foreach (var neighbor in GetNeighbors(terrain, current))
                {
                    if (!cameFrom.ContainsKey(neighbor))
                    {
                        queue.Enqueue(neighbor);
                        cameFrom[neighbor] = current;
                    }
                }
            }

            return null; // No path found
        }

        private IEnumerable<Vector3Int> GetNeighbors(ChunkedTileMap<MetaDataNode> terrain, Vector3Int position)
        {
            var directions = new List<Vector3Int>
            {
                new Vector3Int(1, 0, 0),
                new Vector3Int(-1, 0, 0),
                new Vector3Int(0, 1, 0),
                new Vector3Int(0, -1, 0)
            };

            foreach (var direction in directions)
            {
                var neighbor = position + direction;
                if (!PositionIsOutOfBounds(terrain, neighbor) && terrain.GetTile(neighbor) != null && !terrain.GetTile(neighbor).IsOccupied)
                {
                    yield return neighbor;
                }
            }
        }
    }
}
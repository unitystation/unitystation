using System.Collections.Generic;
using Doors;
using UnityEngine;

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
		///     Gets the most efficent path from point A to B.
		/// </summary>
		/// <param name="terrain">the tilemap that will be used to treverse</param>
		/// <param name="start">starting position of the path</param>
		/// <param name="end">end of path</param>
		/// <param name="allowIncompletePath">Returns a path until the last valid point when a target is unreachable.</param>
		/// <param name="checkForDoors">This guts performance when enabled!!!! be careful!!</param>
		/// <returns>a list of vector3s that create a path from `start` to `end`</returns>
		public List<Vector3Int> FromTo(ChunkedTileMap<MetaDataNode> terrain, Vector3Int start, Vector3Int end,
			bool allowIncompletePath = false, bool checkForDoors = false)
		{
			if (PositionIsOutOfBounds(terrain, start) || PositionIsOutOfBounds(terrain, end))
				return null;

			var queue = new Queue<Vector3Int>();
			var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
			var path = new List<Vector3Int>();
			var lastValidPosition = start;

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

				foreach (var neighbor in GetNeighbors(terrain, current, checkForDoors))
					if (!cameFrom.ContainsKey(neighbor))
					{
						queue.Enqueue(neighbor);
						cameFrom[neighbor] = current;
						lastValidPosition = neighbor;
					}
			}

			if (allowIncompletePath)
			{
				while (lastValidPosition != start)
				{
					path.Add(lastValidPosition);
					lastValidPosition = cameFrom[lastValidPosition];
				}

				path.Add(start);
				path.Reverse();
				return path;
			}

			return null; // No path found
		}

		/// <summary>
		/// Same as FromTo but uses A* algorithm to find the path.
		/// Noticable downgrade in pathfinding quality, but suppousdly faster.
		/// </summary>
		/// <param name="terrain"></param>
		/// <param name="start"></param>
		/// <param name="end"></param>
		/// <param name="checkForDoors">This guts performance when enabled!!!! be careful!!</param>
		/// <param name="allowIncompletePath"></param>
		/// <returns></returns>
		public List<Vector3Int> AStarFromTo(ChunkedTileMap<MetaDataNode> terrain, Vector3Int start, Vector3Int end, bool allowIncompletePath = false, bool checkForDoors = true)
		{
			if (PositionIsOutOfBounds(terrain, start) || PositionIsOutOfBounds(terrain, end))
				return null;

			var fScore = new Dictionary<Vector3Int, float>();
			var openSet = new SortedSet<Vector3Int>(new FScoreComparer(fScore));
			var cameFrom = new Dictionary<Vector3Int, Vector3Int>();
			var gScore = new Dictionary<Vector3Int, float>();
			var path = new List<Vector3Int>();
			Vector3Int lastValidPosition = start;

			openSet.Add(start);
			gScore[start] = 0;
			fScore[start] = Heuristic(start, end);

			while (openSet.Count > 0)
			{
				var current = openSet.Min;
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

				openSet.Remove(current);
				foreach (var neighbor in GetNeighbors(terrain, current, checkForDoors))
				{
					float tentativeGScore = gScore[current] + 1; // Assuming uniform cost
					if (!gScore.ContainsKey(neighbor) || tentativeGScore < gScore[neighbor])
					{
						cameFrom[neighbor] = current;
						gScore[neighbor] = tentativeGScore;
						fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, end);
						openSet.Add(neighbor);
						lastValidPosition = neighbor;
					}
				}
			}

			if (allowIncompletePath)
			{
				while (lastValidPosition != start)
				{
					path.Add(lastValidPosition);
					lastValidPosition = cameFrom[lastValidPosition];
				}
				path.Add(start);
				path.Reverse();
				return path;
			}

			return null; // No path found
		}

		private float Heuristic(Vector3Int a, Vector3Int b)
		{
			return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y); // Manhattan distance
		}

		private class FScoreComparer : IComparer<Vector3Int>
		{
			private readonly Dictionary<Vector3Int, float> fScore;

			public FScoreComparer(Dictionary<Vector3Int, float> fScore)
			{
				this.fScore = fScore;
			}

			public int Compare(Vector3Int a, Vector3Int b)
			{
				if (fScore[a] < fScore[b]) return -1;
				if (fScore[a] > fScore[b]) return 1;
				return 0;
			}
		}

		private IEnumerable<Vector3Int> GetNeighbors(ChunkedTileMap<MetaDataNode> terrain, Vector3Int position, bool checkForDoors)
		{
			var directions = new List<Vector3Int>
			{
				new(1, 0, 0),
				new(-1, 0, 0),
				new(0, 1, 0),
				new(0, -1, 0)
			};

			foreach (var direction in directions)
			{
				var neighbor = position + direction;
				if (PositionIsOutOfBounds(terrain, neighbor)) continue;
				var tile = terrain.GetTile(neighbor);
				if (tile == null) continue;
				if (checkForDoors)
				{
					if (tile.PositionMatrix?.GetFirst<DoorMasterController>(tile.LocalPosition, CustomNetworkManager.IsServer) != null)
					{
						yield return neighbor;
					}
				}
				if (tile is { IsOccupied: false })
				{
					yield return neighbor;
				}
			}
		}
	}
}
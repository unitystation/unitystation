using UnityEngine;
using System.Collections.Generic;
using TileManagement;
using UnityEngine.Tilemaps;

namespace Core.Pathfinding
{
    /// <summary>
    /// A graph interface for use with A* Search.
    /// </summary>
    public interface IGraph
    {
        IEnumerable<Vector3Int> Neighbors(Vector3Int v);
        float Cost(Vector3Int a, Vector3Int b);
    }

    /// <summary>
    /// A graph based off a tilemap where you can move in up, down, left, and right.
    /// </summary>
    public class FourDirectionGraph : IGraph
    {
        MetaTileMap map;

        /// <summary>
        /// Creates a new FourDirectionalGraph using the tilemap's cell bounds
        /// as the graph bounds. If the tilemap does not have tiles on its
        /// intended boundary then the graph bounds could be smaller than it
        /// should be.
        /// </summary>
        public FourDirectionGraph(MetaTileMap map)
        {
            this.map = map;
        }

        public IEnumerable<Vector3Int> Neighbors(Vector3Int v)
        {
            foreach (Vector3Int dir in Utils.FourDirections)
            {
                Vector3Int next = v + dir;

                if (map.IsEmptyAt(next, CustomNetworkManager.IsServer))
                {
                    yield return next;
                }
            }
        }

        public float Cost(Vector3Int a, Vector3Int b)
        {
            return Vector3Int.Distance(a, b);
        }
    }

    /// <summary>
    /// Creates a Graph where characters can move in all 8 directions but cannot
    /// go diagonal if it is across a corner.
    /// </summary>
    public class MoveGraph : IGraph
    {
	    MetaTileMap map;

        public MoveGraph(MetaTileMap map)
        {
            this.map = map;
        }

        public IEnumerable<Vector3Int> Neighbors(Vector3Int v)
        {
            foreach (Vector3Int dir in Utils.FourDirections)
            {
                Vector3Int next = v + dir;

                if (map.IsEmptyAt(next, CustomNetworkManager.IsServer))
                {
                    yield return next;
                }
            }

            foreach (Vector3Int dir in Utils.DiagonalDirections)
            {
                Vector3Int next = v + dir;

                if (map.IsEmptyAt(next, CustomNetworkManager.IsServer))
                {
                    Vector3Int adjacent1 = v + new Vector3Int(dir.x, 0, 0);
                    Vector3Int adjacent2 = v + new Vector3Int(0, dir.y, 0);

                    if (map.IsEmptyAt(adjacent1, CustomNetworkManager.IsServer) && map.IsEmptyAt(adjacent2, CustomNetworkManager.IsServer))
                    {
                        yield return next;
                    }
                }
            }
        }

        public float Cost(Vector3Int a, Vector3Int b)
        {
            return Vector3Int.Distance(a, b);
        }
    }
}

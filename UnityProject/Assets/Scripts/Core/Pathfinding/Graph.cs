using UnityEngine;
using System.Collections.Generic;
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
        Tilemap map;

        /// <summary>
        /// Creates a new FourDirectionalGraph using the tilemap's cell bounds
        /// as the graph bounds. If the tilemap does not have tiles on its
        /// intended boundary then the graph bounds could be smaller than it
        /// should be.
        /// </summary>
        public FourDirectionGraph(Tilemap map)
        {
            this.map = map;
        }

        public IEnumerable<Vector3Int> Neighbors(Vector3Int v)
        {
            foreach (Vector3Int dir in Utils.FourDirections)
            {
                Vector3Int next = v + dir;

                if (map.IsCellEmpty(next))
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
        Tilemap map;

        public MoveGraph(Tilemap map)
        {
            this.map = map;
        }

        public IEnumerable<Vector3Int> Neighbors(Vector3Int v)
        {
            foreach (Vector3Int dir in Utils.FourDirections)
            {
                Vector3Int next = v + dir;

                if (map.IsCellEmpty(next))
                {
                    yield return next;
                }
            }

            foreach (Vector3Int dir in Utils.DiagonalDirections)
            {
                Vector3Int next = v + dir;

                if (map.IsCellEmpty(next))
                {
                    Vector3Int adjacent1 = v + new Vector3Int(dir.x, 0, 0);
                    Vector3Int adjacent2 = v + new Vector3Int(0, dir.y, 0);

                    if (map.IsCellEmpty(adjacent1) && map.IsCellEmpty(adjacent2))
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

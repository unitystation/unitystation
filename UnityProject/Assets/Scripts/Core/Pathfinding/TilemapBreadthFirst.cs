using System;
using System.Collections.Generic;
using System.Linq;
using TileManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Core.Pathfinding
{
    public static class TilemapBreadthFirst
    {
        /// <summary>
        /// A Breadth First Search of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the search</param>
        /// <param name="directions">The directions the traversal can go to find connected nodes</param>
        /// <param name="isGoal">A function to decide if the goal of the search has been found</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static Vector3Int? BreadthFirstSearch(Vector3Int position, Vector3Int[] directions, Func<Vector3Int, bool> isGoal, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(position);

            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

            while (queue.Count > 0)
            {
                Vector3Int node = queue.Dequeue();

                if (isGoal(node))
                {
                    return node;
                }

                foreach (Vector3Int dir in directions)
                {
                    Vector3Int next = node + dir;

                    if (isConnected(node, next) && !visited.Contains(next))
                    {
                        queue.Enqueue(next);
                    }
                }

                visited.Add(node);
            }

            return null;
        }

        /// <summary>
        /// A Breadth First Search of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the search</param>
        /// <param name="directions">The directions the traversal can go to find connected nodes</param>
        /// <param name="isGoal">A function to decide if the goal of the search has been found</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static Vector3Int? BreadthFirstSearch(this MetaTileMap tilemap, Vector3Int position, Vector3Int[] directions, Func<Vector3Int, bool> isGoal, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            return BreadthFirstSearch(position, directions, isGoal, isConnected);
        }

        /// <summary>
        /// A Breadth First Search of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the search</param>
        /// <param name="directions">The directions the traversal can go to find connected nodes</param>
        /// <param name="isGoal">A function to decide if the goal of the search has been found</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static Vector3? BreadthFirstSearch(this MetaTileMap tilemap, Vector3 position, Vector3Int[] directions, Func<Vector3Int, bool> isGoal, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            Vector3Int start = tilemap.WorldToCell(position);

            Vector3Int? resultInt = BreadthFirstSearch(start, directions, isGoal, isConnected);

            Vector3? result = null;

            if (resultInt.HasValue)
            {
                result = tilemap.WorldToCell(resultInt.Value);
            }

            return result;
        }

        /// <summary>
        /// A Breadth First Search of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the search</param>
        /// <param name="isGoal">A function to decide if the goal of the search has been found</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static Vector3Int? BreadthFirstSearch(this MetaTileMap tilemap, Vector3Int position, Func<Vector3Int, bool> isGoal, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            return BreadthFirstSearch(position, Utils.FourDirections, isGoal, isConnected);
        }

        /// <summary>
        /// A Breadth First Search of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the search</param>
        /// <param name="isGoal">A function to decide if the goal of the search has been found</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static Vector3? BreadthFirstSearch(this MetaTileMap tilemap, Vector3 position, Func<Vector3Int, bool> isGoal, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            Vector3Int start = tilemap.WorldToCell(position);

            Vector3Int? resultInt = BreadthFirstSearch(start, Utils.FourDirections, isGoal, isConnected);

            Vector3? result = null;

            if (resultInt.HasValue)
            {
                result = tilemap.WorldToCell(resultInt.Value);
            }

            return result;
        }

        public static Vector3? ClosestEmptyCell(this MetaTileMap tilemap, Vector3 position, Vector3Int[] directions)
        {
            return tilemap.BreadthFirstSearch(
                position,
                directions,
                tilemap.IsCellEmpty,
                (current, next) => true
            );
        }

        public static Vector3? ClosestEmptyCell(this MetaTileMap tilemap, Vector3 position)
        {
            return tilemap.ClosestEmptyCell(position);
        }

        /// <summary>
        /// A Breadth First Traversal of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the traversal</param>
        /// <param name="directions">The directions the traversal can go to find connected nodes</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static List<Vector3Int> BreadthFirstTraversal(Vector3Int position, Vector3Int[] directions, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(position);

            HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

            while (queue.Count > 0)
            {
                Vector3Int node = queue.Dequeue();

                foreach (Vector3Int dir in directions)
                {
                    Vector3Int next = node + dir;

                    if (isConnected(node, next) && !visited.Contains(next))
                    {
                        queue.Enqueue(next);
                    }
                }

                visited.Add(node);
            }

            return visited.ToList();
        }

        /// <summary>
        /// A Breadth First Traversal of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the traversal</param>
        /// <param name="directions">The directions the traversal can go to find connected nodes</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static List<Vector3Int> BreadthFirstTraversal(this MetaTileMap tilemap, Vector3Int position, Vector3Int[] directions, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
	        Vector3Int start = tilemap.WorldToCell(position);

	        List<Vector3Int> positions = BreadthFirstTraversal(start, directions, isConnected);

	        return positions.Select(p => tilemap.WorldToCell(p)).ToList();
        }

        /// <summary>
        /// A Breadth First Traversal of nodes in a grid.
        /// </summary>
        /// <param name="position">Starting position of the traversal</param>
        /// <param name="isConnected">A function to decide if the next node is connected to the current node</param>
        public static List<Vector3Int> BreadthFirstTraversal(this MetaTileMap tilemap, Vector3Int position, Func<Vector3Int, Vector3Int, bool> isConnected)
        {
            return BreadthFirstTraversal(position, Utils.FourDirections, isConnected);
        }
    }
}
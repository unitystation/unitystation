using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using System;

namespace Core.Pathfinding
{
    /// <summary>
    /// A* search based off Amit Patel's implementation.
    /// </summary>
    /// <remarks>
    /// Patel's implementation is different than what you see in most algorithms and AI textbooks.
    /// https://www.redblobgames.com/pathfinding/a-star/implementation.html
    /// </remarks>
    public static class AStar
    {
        /// <summary>
        /// Find a path to the goal or the closest open cell to the goal.
        /// If the self tile is encountered the cell be considered open.
        /// </summary>
        public static LinePath FindLinePathClosest(Tilemap map, Vector3 start, Vector3 goal)
        {
            List<Vector3> path = FindPathClosest(map, start, goal);
            return ToLinePath(path);
        }

        static LinePath ToLinePath(List<Vector3> path)
        {
            LinePath lp = null;

            if (path != null)
            {
                lp = new LinePath(path);
            }

            return lp;
        }

        /// <summary>
        /// Find a path to the goal or the closest open cell to the goal.
        /// If the self tile is encountered the cell be considered open.
        /// </summary>
        public static List<Vector3> FindPathClosest(Tilemap map, Vector3 start, Vector3 goal)
        {
            List<Vector3Int> path = FindPathClosest(map, map.WorldToCell(start), map.WorldToCell(goal));
            return map.GetCellCenterWorld(path);
        }

        /// <summary>
        /// Find a path to the goal or the closest open cell to the goal.
        /// If the self tile is encountered the cell be considered open.
        /// </summary>
        public static List<Vector3Int> FindPathClosest(Tilemap map, Vector3Int start, Vector3Int goal)
        {
            if (!map.IsCellEmpty(goal) && goal != start)
            {
                goal = ClosestCell(OpenCells(map, start, goal), start, goal);
            }

            return AStar.FindPath(new MoveGraph(map), start, goal, Vector3Int.Distance);
        }

        static HashSet<Vector3Int> OpenCells(Tilemap map, Vector3Int start, Vector3Int goal)
        {
            Dictionary<Vector3Int, int> counts = new Dictionary<Vector3Int, int>();
            counts.Add(goal, 0);

            HashSet<Vector3Int> openCells = new HashSet<Vector3Int>();

            float minDist = Mathf.Infinity;
            int minCount = int.MaxValue;

            map.BreadthFirstTraversal(goal, Utils.FourDirections, (current, next) =>
            {
                float dist = Vector3Int.Distance(goal, next);
                int count = counts[current] + 1;
                counts[next] = count;

                if ((map.IsCellEmpty(next) || next == start) && dist <= minDist)
                {
                    minDist = dist;
                    minCount = count;
                    openCells.Add(next);
                }

                return count <= minCount && map.IsInBounds(next);
            });

            return openCells;
        }

        static Vector3Int ClosestCell(HashSet<Vector3Int> openCells, Vector3Int start, Vector3Int goal)
        {
            Vector3Int closest = goal;
            float minDist = Mathf.Infinity;

            foreach (Vector3Int c in openCells)
            {
                float dist = Vector3Int.Distance(start, c);

                if (dist < minDist)
                {
                    minDist = dist;
                    closest = c;
                }
            }

            return closest;
        }

        /// <summary>
        /// Finds a path in the tilemap using world coordinates.
        /// </summary>
        public static LinePath FindLinePath(Tilemap map, Vector3 start, Vector3 goal)
        {
            List<Vector3> path = FindPath(map, start, goal);
            return ToLinePath(path);
        }

        /// <summary>
        /// Finds a path in the tilemap using world coordinates.
        /// </summary>
        public static List<Vector3> FindPath(Tilemap map, Vector3 start, Vector3 goal)
        {
            List<Vector3Int> path = FindPath(map, map.WorldToCell(start), map.WorldToCell(goal));
            return map.GetCellCenterWorld(path);
        }

        /// <summary>
        /// Finds a path in the tilemap using cell coordinates.
        /// </summary>
        public static List<Vector3Int> FindPath(Tilemap map, Vector3Int start, Vector3Int goal)
        {
            return FindPath(new MoveGraph(map), start, goal, Vector3Int.Distance);
        }

        /// <summary>
        /// Finds a path in the graph using cell coordinates.
        /// </summary>
        public static List<Vector3Int> FindPath(IGraph graph, Vector3Int start, Vector3Int goal, Func<Vector3Int, Vector3Int, float> heuristic)
        {
            PriorityQueue<Vector3Int> open = new PriorityQueue<Vector3Int>();
            open.Enqueue(start, 0);

            Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
            cameFrom[start] = start;

            Dictionary<Vector3Int, float> costSoFar = new Dictionary<Vector3Int, float>();
            costSoFar[start] = 0;

            while (open.Count > 0)
            {
                Vector3Int current = open.Dequeue();

                if (current == goal)
                {
                    break;
                }

                foreach (Vector3Int next in graph.Neighbors(current))
                {
                    float newCost = costSoFar[current] + graph.Cost(current, next);

                    if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                    {
                        costSoFar[next] = newCost;
                        float priority = newCost + heuristic(next, goal);
                        open.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }

            List<Vector3Int> path = null;

            if (cameFrom.ContainsKey(goal))
            {
                path = new List<Vector3Int>();

                Vector3Int v = goal;

                while (v != start)
                {
                    RemoveDuplicates(path, v);
                    path.Add(v);
                    v = cameFrom[v];
                }

                RemoveDuplicates(path, start);
                path.Add(start);

                path.Reverse();
            }

            return path;
        }

        static void RemoveDuplicates(List<Vector3Int> path, Vector3Int v)
        {
            if (path.Count >= 2)
            {
                Vector3Int last = path[path.Count - 1];
                Vector3Int secondLast = path[path.Count - 2];

                Vector3Int dir1 = last - secondLast;
                dir1.Clamp(Vector3Int.one * -1, Vector3Int.one);

                Vector3Int dir2 = v - last;
                dir2.Clamp(Vector3Int.one * -1, Vector3Int.one);

                if (dir1 == dir2)
                {
                    path.RemoveAt(path.Count - 1);
                }
            }
        }
    }
}
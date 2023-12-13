using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Core.Pathfinding
{
	public class Pathfinder
    {
        public Grid TileGrid;
        public LayerType BlockingTag = LayerType.Walls;
        public LayerType FloorTag = LayerType.Floors;
        // Start is called before the first frame update
        private NodeGrid ng;
        public Pathfinder(Grid _TileGrid, LayerType _BlockingTag, LayerType _FloorTag)
        {
            TileGrid = _TileGrid;
            BlockingTag = _BlockingTag;
            FloorTag = _FloorTag;
            ng = new NodeGrid(TileGrid, BlockingTag, FloorTag);
        }

        int GetDistance(Node nodeA, Node nodeB)
        {
            int dstX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
            int dstY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

            if (dstX > dstY)
                return 14 * dstY + 10 * (dstX - dstY);
            return 14 * dstX + 10 * (dstY - dstX);
        }

        List<Node> RetracePath(Node startNode, Node endNode)
        {
            List<Node> path = new List<Node>();
            Node currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.parent;
            }
            path.Reverse();

            return path;

        }

        public List<Vector3Int> FindPath(Vector3Int from, Vector3Int to)
        {
            if (from.x + ng.xOffset < 0 || from.y + ng.yOffset < 0 || to.x + ng.xOffset < 0 || to.y + ng.yOffset < 0)
            {
                return new List<Vector3Int>();
            }
            if (from.x + ng.xOffset > ng.nodeArray.GetLength(0) || from.y + ng.yOffset > ng.nodeArray.GetLength(1) || to.x + ng.xOffset > ng.nodeArray.GetLength(0) || to.y + ng.yOffset > ng.nodeArray.GetLength(1))
            {
                return new List<Vector3Int>();
            }
            Node startNode = ng.nodeArray[from.x + ng.xOffset, from.y + ng.yOffset];
            Node targetNode = ng.nodeArray[to.x + ng.xOffset, to.y + ng.yOffset];
            if (startNode == null || startNode.walkable == false || targetNode == null || targetNode.walkable == false)
                return new List<Vector3Int>();

            List<Node> openSet = new List<Node>();
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                Node node = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].fCost < node.fCost || openSet[i].fCost == node.fCost)
                    {
                        if (openSet[i].hCost < node.hCost)
                            node = openSet[i];
                    }
                }

                openSet.Remove(node);
                closedSet.Add(node);

                if (node == targetNode)
                {
                    return NodePathToIntPath(RetracePath(startNode, targetNode));
                }

                foreach (Node neighbour in ng.GetNeighbours(node))
                {
                    if (!neighbour.walkable || closedSet.Contains(neighbour))
                    {
                        continue;
                    }

                    int newCostToNeighbour = node.gCost + GetDistance(node, neighbour);
                    if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                    {
                        neighbour.gCost = newCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = node;

                        if (!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                    }
                }
            }

            return NodePathToIntPath(RetracePath(startNode, targetNode));
        }

        List<Vector3Int> NodePathToIntPath(List<Node> nodePath)
        {
            List<Vector3Int> path = new List<Vector3Int>();
            foreach (Node node in nodePath)
            {
                path.Add(new Vector3Int(node.gridX - ng.xOffset, node.gridY - ng.yOffset, 0));
            }
            return path;
        }
    }
}

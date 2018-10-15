using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace PathFinding
{
	public class PathFinder : MonoBehaviour
	{
		private RegisterTile registerTile;
		private Matrix matrix => registerTile.Matrix;
		private CustomNetTransform customNetTransform;
		public enum Status { idle, searching }
		public Status status;
		private Node startNode;
		private Node goalNode;
		private bool pathFound = false;
		PriorityQueue<Node> frontierNodes;
		private List<Node> exploredNodes = new List<Node>();
		private Dictionary<Vector2Int, Node> allNodes = new Dictionary<Vector2Int, Node>();

		private bool isComplete = false;


		private void Awake()
		{
			registerTile = GetComponent<RegisterTile>();
			customNetTransform = GetComponent<CustomNetTransform>();
		}

		///<summary>
		/// Use local positions related to the matrix the object is on
		///</summary>
		public List<Node> FindNewPath(Vector2Int startPos, Vector2Int targetPos)
		{
			pathFound = false;

			startNode = new Node {
				position = startPos,
				distanceTraveled = 0
			};

			goalNode = new Node
			{
				position = targetPos
			};

			isComplete = false;

			frontierNodes = new PriorityQueue<Node>();
			frontierNodes.Enqueue(startNode);
			exploredNodes.Clear();
			allNodes.Clear();
			allNodes.Add(startNode.position, startNode);
			allNodes.Add(goalNode.position, goalNode);
			return SearchForRoute();
		}

		List<Node> SearchForRoute()
		{
			status = Status.searching;

			while (!isComplete)
			{
				if (frontierNodes.Count > 0)
				{
					Node currentNode = frontierNodes.Dequeue();

					if (currentNode.neighbors == null)
					{
						FindNeighbours(currentNode);
					}

					if (!exploredNodes.Contains(currentNode))
					{
						exploredNodes.Add(currentNode);
					}

					ExpandFrontierAStar(currentNode);

					if (pathFound)
					{
						isComplete = true;
						List<Node> path = new List<Node>();

						Node nextNode = goalNode;

						do {
							path.Add(nextNode);
							nextNode = nextNode.previous;
						}
						while (nextNode != null);

						path.Reverse();

						return path;
					}
				}
				else
				{
					isComplete = true;
					return null;
				}
			}
			
			status = Status.idle;
			Debug.Log("Search complete");
			return null;
		}

		void FindNeighbours(Node currentNode)
		{
			Vector2Int startPos = currentNode.position + new Vector2Int(-1, 1);
			List<Node> newNeighbours = new List<Node>(8);

			for (int i = 0; i < 3; i++)
			{
				for (int y = 0; y < 3; y++)
				{
					Vector2Int searchPos = new Vector2Int(startPos.x + i, startPos.y - y);
					if (searchPos == currentNode.position)
					{
						continue;
					}
					if (!allNodes.ContainsKey(searchPos))
					{
						Node newNode = new Node
						{
							position = searchPos
						};
						allNodes.Add(newNode.position, newNode);
						newNeighbours.Add(newNode);
					}
					else
					{
						newNeighbours.Add(allNodes[searchPos]);
					}
				}
			}
			
			currentNode.neighbors = newNeighbours;
		}

		private void ExpandFrontierAStar(Node node)
		{
			if (node == null)
				return;

			for (int i = 0; i < node.neighbors.Count; i++)
			{
				Node neighbor = node.neighbors[i];

				if (exploredNodes.Contains(neighbor))
					continue;

				RefreshNodeType(neighbor);

				if(node.neighbors[i].nodeType == NodeType.Blocked)
					continue;

				float distanceToNeighbor = GetNodeDistance(node, neighbor);
				float newDistanceTraveled = distanceToNeighbor + node.distanceTraveled + (int)node.nodeType;
				uint newPriority = (uint)(100 * (newDistanceTraveled + GetNodeDistance(neighbor, goalNode)));

				if (neighbor.priority > newPriority) {
					neighbor.previous = node;
					neighbor.distanceTraveled = newDistanceTraveled;
					neighbor.priority = newPriority;

					if (node == goalNode)
						pathFound = true;
					else {
						frontierNodes.Remove(neighbor); // Re-sort if the node existed already.
						frontierNodes.Enqueue(neighbor);
					}
				}
			}
		}

		private void RefreshNodeType(Node node)
		{
			Vector3Int checkPos = new Vector3Int(node.position.x,
				node.position.y, 0);

			if (matrix.IsPassableAt(checkPos))
			{
				node.nodeType = NodeType.Open;
				return;
			}
			else
			{
				var getDoor = matrix.GetFirst<DoorController>(checkPos);
				if (!getDoor)
				{
					//TODO: Door consideration will work if you change this condition back to doors
					//It is off for the time being as npcs that can't use doors keeps trying to make a path through them

					//node.nodeType = NodeType.Door; 

					//So block doors for the meantime:
					node.nodeType = NodeType.Blocked;
				}
				else
				{
					node.nodeType = NodeType.Blocked;
				}
			}
		}

		private static float GetNodeDistance(Node source, Node target)
		{
			int dx = Mathf.Abs(source.position.x - target.position.x);
			int dy = Mathf.Abs(source.position.y - target.position.y);

			int min = Mathf.Min(dx, dy);
			int max = Mathf.Max(dx, dy);

			int diagonalSteps = min;
			int straightSteps = max - min;

			return (1.4f * diagonalSteps + straightSteps);
		}
	}
}
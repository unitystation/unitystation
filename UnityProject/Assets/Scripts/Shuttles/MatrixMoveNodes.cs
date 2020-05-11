using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatrixMoveNodes
{
	public Vector2Int[] nodes = new Vector2Int[4];
	public HistoryNode[] historyNodes = new HistoryNode[4];

	/// <summary>
	/// Generates new travel nodes based on the start position
	/// and direction of travel. Also automatically clears history nodes
	/// </summary>
	public void GenerateMoveNodes(Vector2 fromPosition, Vector2Int direction)
	{
		ResetHistoryNodes();
		var lastNode = Vector2Int.RoundToInt(fromPosition);
		for (int i = 0; i < nodes.Length; i++)
		{
			var node = lastNode + direction;
			lastNode = node;
			nodes[i] = node;
		}
	}

	/// <summary>
	/// When getting a new target node provide the direction of movement
	/// so a new node can be placed at the end of the node list
	/// </summary>
	public Vector2Int GetTargetNode(Vector2Int directionOfMove)
	{
		var targetNode = nodes[0];
		for (int i = 1; i < nodes.Length; i++)
		{
			nodes[i - 1] = nodes[i];
			if (i == nodes.Length - 1)
			{
				nodes[i] = nodes[i - 1] + directionOfMove;
			}
		}

		return targetNode;
	}

	/// <summary>
	/// Used by the RCS system to adjust nodes when in flight
	/// to give the effect of strafing
	/// </summary>
	public void AdjustFutureNodes(Vector2Int adjustDir)
	{
		for (int i = 0; i < nodes.Length; i++)
		{
			nodes[i] += adjustDir;
		}
	}

	/// <summary>
	/// Clears history nodes
	/// </summary>
	public void ResetHistoryNodes()
	{
		for (int i = 0; i < historyNodes.Length; i++)
		{
			historyNodes[i] = new HistoryNode().GenerateBlankNode();
		}
	}

	/// <summary>
	/// Add a new history node to the array
	/// Requires the position of the node and the network time
	/// that the node was reached
	/// </summary>
	public void AddHistoryNode(Vector2Int position, double networkTime)
	{
		for (int i = historyNodes.Length - 2; i >= 0; i--)
		{
			historyNodes[i + 1] = historyNodes[i];
			if (i == 0)
			{
				historyNodes[i] = new HistoryNode
				{
					nodePos = position,
					networkTime = networkTime
				};
			}
		}
	}
}

public struct HistoryNode
{
	public Vector2Int nodePos;
	public double networkTime;

	public HistoryNode GenerateBlankNode()
	{
		nodePos = Vector2Int.zero;
		networkTime = -1;
		return this;
	}
}

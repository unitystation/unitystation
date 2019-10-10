using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Diagnostics;

namespace PathFinding
{
	public enum NodeType
	{
		Open = 0,
		Blocked = 1,
		Door = 2
	}

	[DebuggerDisplay("Node @{position}, P={priority}")]
	public class Node : IComparable<Node>
	{
		public NodeType nodeType = NodeType.Open;

		public Vector2Int position; //Localpos 

		public List<Node> neighbors;
		public float distanceTraveled = Mathf.Infinity;
		public Node previous = null;
		public uint priority = uint.MaxValue;

		public int CompareTo(Node other)
		{
			return priority.CompareTo(other.priority);
		}
	}
}

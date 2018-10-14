using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace PathFinding
{
	public enum NodeType
	{
		Open = 0,
		Blocked = 1,
		Door = 2
	}

	public class Node : IComparable<Node>
	{
		public NodeType nodeType = NodeType.Open;

		public Vector2Int position; //Localpos 

		public List<Node> neighbors = new List<Node>();
		public float distanceTraveled = Mathf.Infinity;
		public Node previous = null;
		public int priority;

		public int CompareTo(Node other)
		{
			if (this.priority < other.priority)
			{
				return -1;

			}
			else if (this.priority > other.priority)
			{
				return 1;

			}
			else
			{
				return 0;
			}
		}
	}
}

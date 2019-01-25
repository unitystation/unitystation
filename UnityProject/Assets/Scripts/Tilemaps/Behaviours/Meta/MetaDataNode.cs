using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using UnityEngine;

public enum NodeType
{
	None,
	Space,
	Room,
	Occupied
}

public class MetaDataNode
{
	public static readonly MetaDataNode None = new MetaDataNode(Vector3Int.one * -1000000);

	public readonly Vector3Int Position;

	public NodeType Type;

	public GasMix Atmos;

	public Hotspot Hotspot;

	public int Damage;

	public int NeighborCount
	{
		get
		{
			lock (neighbors)
			{
				return neighbors.Count;
			}
		}
	}

	public MetaDataNode[] Neighbors
	{
		get
		{
			lock (neighbors)
			{
				return neighbors.ToArray();
			}
		}
	}

	private List<MetaDataNode> neighbors;

	public MetaDataNode(Vector3Int position)
	{
		Position = position;
		neighbors = new List<MetaDataNode>();
		Atmos = GasMixes.Space;
	}

	public bool IsSpace => Type == NodeType.Space;
	public bool IsRoom => Type == NodeType.Room;
	public bool IsOccupied => Type == NodeType.Occupied;

	public bool Exists => this != None;

	public void AddNeighborsToList(ref List<MetaDataNode> list)
	{
		lock (neighbors)
		{
			foreach (MetaDataNode neighbor in neighbors)
			{
				list.Add(neighbor);
			}
		}
	}

	public void ClearNeighbors()
	{
		lock (neighbors)
		{
			neighbors.Clear();
		}
	}

	public void AddNeighbor(MetaDataNode neighbor)
	{
		if (neighbor != this)
		{
			lock (neighbors)
			{
				neighbors.Add(neighbor);
			}
		}
	}

	public bool HasHotspot => Hotspot != null;

	public void RemoveNeighbor(MetaDataNode neighbor)
	{
		lock (neighbors)
		{
			neighbors.Remove(neighbor);
		}
	}

	public string WindowDmgType { get; set; } = "";

	public void ResetDamage()
	{
		Damage = 0;
	}

	public override string ToString()
	{
		return Position.ToString();
	}
}
using System;
using System.Collections.Generic;
using Atmospherics;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;


public enum NodeType
{
	Space,
	Room,
	Occupied
}

[Serializable]
public class MetaDataNode
{
	public static readonly MetaDataNode None = new MetaDataNode(Vector3Int.zero) {Room = -1};

	public readonly Vector3Int Position;

	public readonly HashSet<MetaDataNode> Neighbors;

	public GasMix Atmos;

	public int Room; // TODO

	public NodeType Type;

	public MetaDataNode(Vector3Int position)
	{
		Position = position;
		Neighbors = new HashSet<MetaDataNode>();
		Atmos = GasMixUtils.Space;
	}

	public bool IsSpace => Type == NodeType.Space;
	public bool IsRoom => Type == NodeType.Room;
	public bool IsOccupied => Type == NodeType.Occupied;

	public bool Exists => this != None;

	public void ClearNeighbors()
	{
		foreach (MetaDataNode neighbor in Neighbors)
		{
			neighbor.RemoveNeighbor(this);
		}

		Neighbors.Clear();
	}

	public void AddNeighbor(MetaDataNode neighbor)
	{
		Neighbors.Add(neighbor);
	}

	public void RemoveNeighbor(MetaDataNode neighbor)
	{
		Neighbors.Remove(neighbor);
	}
}
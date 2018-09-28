using System;
using System.Collections.Generic;
using System.Linq;
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

	private HashSet<MetaDataNode> neighbors;
	private MetaDataNode[] Neighbors;

	public GasMix Atmos;

	public int Room; // TODO

	public NodeType Type;

	public MetaDataNode(Vector3Int position)
	{
		Position = position;
		neighbors = new HashSet<MetaDataNode>();
		Neighbors = new MetaDataNode[0];
		Atmos = GasMixUtils.Space;
	}

	public bool IsSpace => Type == NodeType.Space;
	public bool IsRoom => Type == NodeType.Room;
	public bool IsOccupied => Type == NodeType.Occupied;

	public bool Exists => this != None;

	public MetaDataNode[] GetNeighbors()
	{
		return Neighbors;
	}

	public void ClearNeighbors()
	{
		foreach (MetaDataNode neighbor in neighbors)
		{
			neighbor.RemoveNeighbor(this);
		}

		neighbors.Clear();

		Neighbors = neighbors.ToArray();
	}

	public void AddNeighbor(MetaDataNode neighbor)
	{
		neighbors.Add(neighbor);

		Neighbors = neighbors.ToArray();
	}

	public void RemoveNeighbor(MetaDataNode neighbor)
	{
		neighbors.Remove(neighbor);

		Neighbors = neighbors.ToArray();
	}

	private int damage = 0;

	public string WindowDmgType { get; set; } = "";

	public void Reset()
	{
		Room = 0;
	}

	public void ResetDamage()
	{
		damage = 0;
	}

	public int GetDamage
	{
		get { return damage; }
	}

	public void AddDamage(int amt)
	{
		damage += amt;
	}
}
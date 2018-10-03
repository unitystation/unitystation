using System;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Tilemaps.Behaviours.Meta;
using Tilemaps.Behaviours.Meta.Utils;
using UnityEngine;
using UnityEngine.Serialization;


public enum NodeType
{
	Space,
	Room,
	Occupied
}

[Serializable]
public class MetaDataNode
{
	public static readonly MetaDataNode None = new MetaDataNode(Vector3Int.one * -1000000);

	public readonly Vector3Int Position;

	private HashSet<MetaDataNode> neighbors;
	private MetaDataNode[] Neighbors;

	public NodeType Type;

	public GasMix Atmos;

	public int Damage;

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
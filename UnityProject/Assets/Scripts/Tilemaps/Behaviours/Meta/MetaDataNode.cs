using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Atmospherics;
using Explosions;
using Radiation;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

/// <summary>
/// Holds all of the metadata associated with an individual tile, such as for atmospherics simulation, damage.
/// </summary>
public class MetaDataNode : IGasMixContainer
{
	public static readonly MetaDataNode None;

	/// <summary>
	/// Used for calculating explosion data
	/// </summary>
	public ExplosionNode ExplosionNode = null;

	/// <summary>
	/// Used for storing useful information for the radiation system and The radiation level
	/// </summary>
	public RadiationNode RadiationNode = new RadiationNode();

	/// <summary>
	/// Contains all electrical data for this tile
	/// </summary>
	public List<ElectricalMetaData> ElectricalData = new List<ElectricalMetaData>();

	/// <summary>
	/// This contains all the pipe data needed On the tile
	/// </summary>
	public List<Pipes.PipeNode> PipeData = new List<Pipes.PipeNode>();


	/// <summary>
	/// Local position of this tile in its parent matrix.
	/// </summary>
	public readonly Vector3Int Position;

	/// <summary>
	/// If this node is in a closed room, it's assigned to it by the room's number
	/// </summary>
	public int RoomNumber = -1;

	/// <summary>
	/// Type of this node.
	/// </summary>
	public NodeType Type;

	/// <summary>
	/// The mixture of gases currently on this node.
	/// </summary>
	public GasMix GasMix { get; set; }

	/// <summary>
	/// The hotspot state of this node - indicates a potential to ignite gases, and
	/// ignites them if conditions are met. Null if no potential exists on this tile.
	/// </summary>
	public Hotspot Hotspot;

	private Dictionary<LayerType, float> damageInfo  = new Dictionary<LayerType, float>();

	public float GetTileDamage(LayerType layerType)
	{
		TryCreateDamageInfo(layerType);
		return damageInfo[layerType];
	}

	public void AddTileDamage(LayerType layerType, float damage)
	{
		TryCreateDamageInfo(layerType);
		damageInfo[layerType] += damage;
	}

	public void RemoveTileDamage(LayerType layerType)
	{
		damageInfo.Remove(layerType);
	}

	public void ResetDamage(LayerType layerType)
	{
		TryCreateDamageInfo(layerType);
		damageInfo[layerType] = 0;
	}

	public void TryCreateDamageInfo(LayerType layerType)
	{
		if (damageInfo.ContainsKey(layerType)) return;
		damageInfo.Add(layerType, 0);
	}

	/// <summary>
	/// Direction of wind in local coordinates
	/// </summary>
	public Vector2Int 	WindDirection 	= Vector2Int.zero;
	public float		WindForce 		= 0;

	/// <summary>
	/// Number of neighboring MetaDataNodes
	/// </summary>
	public int NeighborCount { get; private set; }

	/// <summary>
	/// Current drying coroutine.
	/// </summary>
	public IEnumerator CurrentDrying;

	/// <summary>
	/// The current neighbor nodes. Nodes can be Null!
	/// </summary>
	public readonly MetaDataNode[] Neighbors = new MetaDataNode[4];

	private List<MetaDataNode> neighborList;

	public ReactionManager ReactionManager => reactionManager;
	private ReactionManager reactionManager;


	/// <summary>
	/// Create a new MetaDataNode on the specified local position (within the parent matrix)
	/// </summary>
	/// <param name="position">local position (within the matrix) the node exists on</param>
	public MetaDataNode(Vector3Int position, ReactionManager reactionManager)
	{
		Position = position;
		neighborList = new List<MetaDataNode>(4);
		for (var i = 0; i < neighborList.Capacity; i++)
		{
			neighborList.Add(null);
		}
		GasMix = new GasMix(GasMixes.Space);
		this.reactionManager = reactionManager;
	}

	static MetaDataNode()
	{
		None = new MetaDataNode(Vector3Int.one * -1000000, null);
	}

	/// <summary>
	/// Is this tile in space
	/// </summary>
	public bool IsSpace => Type == NodeType.Space;

	/// <summary>
	/// Is this tile in a room
	/// </summary>
	public bool IsRoom => Type == NodeType.Room;

	/// <summary>
	/// Does this tile contain a closed airlock/shutters?
	/// (used for gas freezing)
	/// </summary>
	public bool IsClosedAirlock { get; set; }

	/// <summary>
	/// Is this tile occupied by something impassable (airtight!)
	/// </summary>
	public bool IsOccupied => Type == NodeType.Occupied;

	public bool IsSlippery = false;

	public bool Exists => this != None;

	public void AddNeighborsToList(ref List<MetaDataNode> list)
	{
		lock (neighborList)
		{
			foreach (MetaDataNode neighbor in neighborList)
			{
				if (neighbor != null && neighbor.Exists)
				{
					list.Add(neighbor);
				}
			}
		}
	}

	public void ClearNeighbors()
	{
		lock (neighborList)
		{
			for (int i = 0; i < 4; i++)
			{
				neighborList[i] = null;
			}
		}
	}

	public bool IsNeighbourToNonSpace()
	{
		lock (neighborList)
		{
			for (int i = 0; i < 4; i++)
			{
				if (neighborList[i].IsSpace == false)
				{
					return true;
				}
			}
		}

		return false;
	}

	public void AddNeighbor(MetaDataNode neighbor, Vector3Int direction)
	{
		if (neighbor != this)
		{
			lock (neighborList)
			{
				bool added = false;
				for (var i = 0; i < MetaUtils.Directions.Length; i++)
				{
					if (MetaUtils.Directions[i] == direction)
					{
						neighborList[i] = neighbor;
						added = true;
						break;
					}
				}

				if (added)
				{
					SyncNeighbors();
					return;
				}
				Logger.LogErrorFormat("Failed adding neighbor {0} to node {1} at direction {2}", Category.Atmos, neighbor, this, direction);
			}
		}
	}

	public bool HasHotspot => Hotspot != null;
	public bool HasWind => WindDirection != Vector2Int.zero;

	public void RemoveNeighbor(MetaDataNode neighbor)
	{
		lock (neighborList)
		{
			if (neighborList.Contains(neighbor))
			{
				neighborList[neighborList.IndexOf(neighbor)] = null;

				SyncNeighbors();
			}
		}
	}

	public override string ToString()
	{
		return Position.ToString();
	}

	private void SyncNeighbors()
	{
		for (int i = 0, j = 0; i < Neighbors.Length; i++)
		{
			Neighbors[i] = neighborList[i];

			if (Neighbors[i] != null)
			{
				j++;
				NeighborCount = j;
			}
		}
	}
}
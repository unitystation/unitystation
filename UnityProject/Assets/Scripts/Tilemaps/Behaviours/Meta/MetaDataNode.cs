using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Detective;
using Logs;
using UnityEngine;
using ScriptableObjects.Atmospherics;
using Tilemaps.Behaviours.Meta;
using Systems.Atmospherics;
using Systems.Electricity;
using Systems.Explosions;
using Systems.Pipes;
using Systems.Radiation;
using Systems.DisposalPipes;


/// <summary>
/// Holds all of the metadata associated with an individual tile, such as for atmospherics simulation, damage.
/// </summary>
public class MetaDataNode : IGasMixContainer
{
	public static readonly MetaDataNode None;

	/// <summary>
	/// MetaDataSystem That is part of
	/// </summary>
	public MetaDataSystem MetaDataSystem;

	/// <summary>
	/// Contains the matrix of the current node
	/// </summary>
	public Matrix PositionMatrix = null;

	/// <summary>
	/// Used for calculating explosion data
	/// </summary>
	public ExplosionNode[] ExplosionNodes = new ExplosionNode[5];

	private RadiationNode radiationNode;

	/// <summary>
	/// Used for storing useful information for the radiation system and The radiation level
	/// </summary>
	public RadiationNode RadiationNode
	{
		get
		{
			if (radiationNode != null) return radiationNode;

			radiationNode = new RadiationNode();

			return radiationNode;
		}
	}

	/// <summary>
	/// Contains all electrical data for this tile
	/// </summary>
	public List<ElectricalMetaData> ElectricalData = new List<ElectricalMetaData>();

	/// <summary>
	/// This contains all the pipe data needed On the tile
	/// </summary>
	public List<PipeNode> PipeData = new List<PipeNode>();

	/// <summary>
	/// This contains all the disposal pipe data needed On the tile
	/// </summary>
	public List<DisposalPipeNode> DisposalPipeData = new List<DisposalPipeNode>();

	/// <summary>
	/// Local position of this tile in its parent matrix.
	/// </summary>
	public readonly Vector3Int LocalPosition;

	/// <summary>
	/// World position of this tile in its parent matrix.
	/// </summary>
	public Vector3Int WorldPosition => LocalPosition.ToWorldInt(PositionMatrix);

	/// <summary>
	/// If this node is in a closed room, it's assigned to it by the room's number
	/// </summary>
	public int RoomNumber = -1;

	/// <summary>
	/// Type of this node.
	/// </summary>
	public NodeType Type;

	/// <summary>
	/// Occupied Type of this node.
	/// </summary>
	public NodeOccupiedType OccupiedType;

	private GasMix gasMix;

	/// <summary>
	/// The mixture of gases currently on this node.
	/// </summary>
	public GasMix GasMix
	{
		get
		{
			if (gasMix != null) return gasMix;

			gasMix = GasMix.NewGasMix(GasMixes.BaseSpaceMix);

			return gasMix;
		}

		set => gasMix = value;
	}

	/// <summary>
	/// The hotspot state of this node - indicates a potential to ignite gases, and
	/// ignites them if conditions are met. Null if no potential exists on this tile.
	/// </summary>
	public Hotspot Hotspot;

	private Dictionary<LayerType, float> damageInfo  = new Dictionary<LayerType, float>();

	//Which overlays this node has on
	private HashSet<GasSO> gasOverlayData = new HashSet<GasSO>();
	public HashSet<GasSO> GasOverlayData => gasOverlayData;

	public AppliedDetails AppliedDetails = new AppliedDetails();


	private SmokeNode smokeNode;
	public SmokeNode SmokeNode
	{
		get
		{
			if (smokeNode != null) return smokeNode;

			smokeNode = new SmokeNode()
			{
				OnMetaDataNode = this
			};

			return smokeNode;
		}
	}

	private FoamNode foamNode;
	public FoamNode FoamNode
	{
		get
		{
			if (foamNode != null) return foamNode;

			foamNode = new FoamNode()
			{
				OnMetaDataNode = this
			};

			return foamNode;
		}
	}


	//Conductivity Stuff//

	//Temperature of the solid node
	public float ConductivityTemperature = TemperatureUtils.ZERO_CELSIUS_IN_KELVIN;
	//How easily the node conducts 0-1
	public float ThermalConductivity = 0f;
	//Heat capacity of the node, also effects conducting speed
	public float HeatCapacity = 0f;

	//If this node started the conductivity
	public bool StartingSuperConduct;
	//If this node is allowed to share temperature to surrounding nodes
	public bool AllowedToSuperConduct;

	//How long since the last wind spot particle was spawned
	//This is here as dictionaries are a pain and for performance but costs more memory
	public float windEffectTime = 0;

	public void AddGasOverlay(GasSO gas)
	{
		gasOverlayData.Add(gas);
	}

	public void RemoveGasOverlay(GasSO gas)
	{
		gasOverlayData.Remove(gas);
	}

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

	public readonly Vector2[] WindData = new Vector2[(int)Enum.GetValues(typeof(PushType)).Cast<PushType>().Max() +1 ];
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
	/// <param name="localPosition">local position (within the matrix) the node exists on</param>
	public MetaDataNode(Vector3Int localPosition, ReactionManager reactionManager, Matrix matrix, MetaDataSystem InMetaDataSystem )
	{
		MetaDataSystem = InMetaDataSystem;
		PositionMatrix = matrix;
		LocalPosition = localPosition;

		neighborList = new List<MetaDataNode>(4);
		for (var i = 0; i < neighborList.Capacity; i++)
		{
			neighborList.Add(null);
		}

		this.reactionManager = reactionManager;
	}

	static MetaDataNode()
	{
		None = new MetaDataNode(Vector3Int.one * -1000000, null, null, null);
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
	/// Does this tile contain a closed airlock/shutters? Prevents gas exchange to adjacent tiles
	/// (used for gas freezing)
	/// </summary>
	public bool IsIsolatedNode => OccupiedType == NodeOccupiedType.Full;

	/// <summary>
	/// Is this tile occupied by something impassable (airtight!)
	/// </summary>
	public bool IsOccupied => Type == NodeType.Occupied;


	private bool isSlippery = false;

	public bool IsSlippery
	{
		get
		{
			return isSlippery;
		}
		set
		{
			isSlippery = value;
			ForceUpdateClient();
		}
	}

	public bool Exists => this != None;

	public void AddNeighborsToList(ref List<(MetaDataNode, bool)> list)
	{
		lock (neighborList)
		{
			foreach (MetaDataNode neighbor in neighborList)
			{
				if (neighbor == null || neighbor.Exists == false) continue;

				//Bool means to block gas equalise, e.g for when closed windoor/directional passable
				//Have to do IsOccupiedBlocked from both tiles perspective
				var equalise = neighbor.IsOccupied == false && neighbor.IsIsolatedNode == false
						&& IsOccupiedBlocked(neighbor) == false && neighbor.IsOccupiedBlocked(this) == false;
				list.Add((neighbor, equalise));
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
				Loggy.LogErrorFormat("Failed adding neighbor {0} to node {1} at direction {2}", Category.Matrix, neighbor, this, direction);
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

	public void ChangeGasMix(GasMix newGasMix)
	{
		AtmosSimulation.RemovalAllGasOverlays(this);

		GasMix = newGasMix;

		AtmosSimulation.GasVisualEffects(this);
	}

	public override string ToString()
	{
		return LocalPosition.ToString();
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

	public void ForceUpdateClient()
	{
		PositionMatrix.MetaDataLayer.AddNetworkChange(LocalPosition, this);
	}

	public bool IsOccupiedBlocked(MetaDataNode neighbourNode)
	{
		if (OccupiedType == NodeOccupiedType.None) return false;
		if (OccupiedType == NodeOccupiedType.Full) return true;

		var direction =  neighbourNode.LocalPosition - LocalPosition;
		var orientationEnum = Orientation.FromAsEnum(direction.To2());

		var occupied = NodeOccupiedUtil.DirectionEnumToOccupied(orientationEnum);

		var result = OccupiedType.HasFlag(occupied);

		//Note HasFlag might not be the best way to check, could be slower than making If statement ourselves
		return result;
	}
}


public enum PushType
{
	Wind = 1,
	Conveyor = 2
}

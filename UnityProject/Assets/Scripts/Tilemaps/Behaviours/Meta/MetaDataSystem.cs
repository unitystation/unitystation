using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Systems.Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using Mirror;

/// <summary>
/// Subsystem behavior which manages updating the MetaDataNodes and simulation that affects them for a given matrix.
/// </summary>
public class MetaDataSystem : SubsystemBehaviour
{
	// for Conditional updating
	public override SystemType SubsystemType =>SystemType.MetaDataSystem;

	// Set higher priority to ensure that it is executed before other systems
	public override int Priority => 100;

	/// <summary>
	/// Nodes which exist in space next to room tiles of the matrix.
	/// These are regularly checked for tiles from other matrices, i.e. they build the bridge to other matrices
	/// </summary>
	private ConcurrentDictionary<MetaDataNode, MetaDataNode> externalNodes;

	/// <summary>
	/// Matrix this system is managing the MetaDataNodes for.
	/// </summary>
	private Matrix matrix;

	private int roomCounter = 0;

	public override void Awake()
	{
		base.Awake();

		matrix = GetComponentInChildren<Matrix>(true);
		externalNodes = new ConcurrentDictionary<MetaDataNode, MetaDataNode>();
	}

	private void OnEnable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Add(CallbackType.UPDATE, ServerUpdateMe);
		}
	}

	private void OnDisable()
	{
		if (CustomNetworkManager.IsServer)
		{
			UpdateManager.Remove(CallbackType.UPDATE, ServerUpdateMe);
		}
	}

	[Server]
	public override void Initialize()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		if (MatrixManager.IsInitialized)
		{
			LocateRooms();
			Stopwatch Dsw = new Stopwatch();
			Dsw.Start();
			matrix.MetaTileMap.InitialiseUnderFloorUtilities(CustomNetworkManager.IsServer);
			Dsw.Stop();
			Logger.Log($"Initialise {gameObject.name} Utilities (Power cables, Atmos pipes): " + Dsw.ElapsedMilliseconds + " ms", Category.Matrix);
		}

		sw.Stop();

		Logger.Log($"{gameObject.name} MetaData init: " + sw.ElapsedMilliseconds + " ms", Category.Matrix);
	}

	public override void UpdateAt(Vector3Int localPosition)
	{
		MetaDataNode node = metaDataLayer.Get(localPosition);

		MetaUtils.RemoveFromNeighbors(node);
		externalNodes.TryRemove(node, out MetaDataNode nothing);

		node.IsIsolatedNode = false;

		// If the node is atmos passable (i.e. space or room), we need to setup its neighbors again, otherwise it's occupied and does need a neighbor check
		if (metaTileMap.IsAtmosPassableAt(localPosition, true))
		{
			node.ClearNeighbors();
			node.Type = metaTileMap.IsSpaceAt(localPosition, true) ? NodeType.Space : NodeType.Room;

			if (node.Type == NodeType.Space)
			{
				node.ThermalConductivity = AtmosDefines.SPACE_THERMAL_CONDUCTIVITY;
				node.HeatCapacity =  AtmosDefines.SPACE_HEAT_CAPACITY;
			}
		}
		else
		{
			node.Type = NodeType.Occupied;
			if (matrix.GetFirst<RegisterDoor>(localPosition, true))
			{
				node.IsIsolatedNode = true;

				//TODO hard coded these values, might be better to put them in register door?
				node.ThermalConductivity = 0.001f;
				node.HeatCapacity =  10000f;
			}
		}

		if (node.IsIsolatedNode == false && node.Type != NodeType.Space &&
		    matrix.MetaTileMap.GetTile(localPosition, true) is BasicTile tile && tile != null)
		{
			node.HeatCapacity = tile.HeatCapacity;
			node.ThermalConductivity = tile.ThermalConductivity;
		}

		SetupNeighbors(node);
		MetaUtils.AddToNeighbors(node);
	}

	private void LocateRooms()
	{
		BoundsInt bounds = metaTileMap.GetBounds();

		var watch = new Stopwatch();
		watch.Start();
		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			FindRoomAt(position);
		}
		Logger.LogFormat("Created rooms in {0}ms", Category.TileMaps, watch.ElapsedMilliseconds);
	}

	private void FindRoomAt(Vector3Int position)
	{
		if (!metaTileMap.IsAtmosPassableAt(position, true))
		{
			MetaDataNode node = metaDataLayer.Get(position);
			node.Type = NodeType.Occupied;

			if (matrix.GetFirst<RegisterDoor>(position, true))
			{
				node.IsIsolatedNode = true;
				node.ThermalConductivity = 0.0001f;
				node.HeatCapacity =  10000f;
			}
			else if (matrix.MetaTileMap.GetTile(position, true) is BasicTile tile && tile != null)
			{
				node.HeatCapacity = tile.HeatCapacity;
				node.ThermalConductivity = tile.ThermalConductivity;
			}

			SetupNeighbors(node);
		}
		else if (!metaTileMap.IsSpaceAt(position, true) && !metaDataLayer.IsRoomAt(position) && !metaDataLayer.IsSpaceAt(position))
		{
			CreateRoom(position);
		}
	}

	private void CreateRoom(Vector3Int origin)
	{
		if (metaDataLayer.Get(origin).RoomNumber != -1)
		{
			return;
		}
		var roomPositions = new HashSet<Vector3Int>();
		var freePositions = new UniqueQueue<Vector3Int>();

		freePositions.Enqueue(origin);

		var isSpace = false;

		// breadth-first search of the connected tiles that are not occupied
		while (!freePositions.IsEmpty)
		{
			if (freePositions.TryDequeue(out Vector3Int position))
			{
				roomPositions.Add(position);

				Vector3Int[] neighbors = MetaUtils.GetNeighbors(position, null);
				for (var i = 0; i < neighbors.Length; i++)
				{
					Vector3Int neighbor = neighbors[i];
					if (metaTileMap.IsSpaceAt(neighbor, true))
					{
						Vector3Int worldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

						// If matrix manager says, the neighboring positions is space, the whole room is connected to space.
						// Otherwise there is another matrix, blocking off the connection to space.
						if (MatrixManager.IsSpaceAt(worldPosition, true))
						{
							isSpace = true;
						}
					}
					else if (metaTileMap.IsAtmosPassableAt(position, neighbor, true))
					{
						// if neighbor position is not yet a room in the meta data layer and not in the room positions list,
						// add it to the positions that need be checked
						if (!roomPositions.Contains(neighbor) && !metaDataLayer.IsRoomAt(neighbor))
						{
							freePositions.Enqueue(neighbor);
						}
					}
				}
			}
		}

		AssignType(roomPositions, isSpace ? NodeType.Space : NodeType.Room);

		SetupNeighbors(roomPositions);
	}

	private void AssignType(IEnumerable<Vector3Int> positions, NodeType nodeType)
	{
		// Bulk assign type to nodes at given positions
		foreach (Vector3Int position in positions)
		{
			MetaDataNode node = metaDataLayer.Get(position);

			node.Type = nodeType;

			// assign room number, if type is room
			node.RoomNumber = nodeType == NodeType.Room ? roomCounter : -1;
		}

		// increase room counter, so next room will get a new number
		if (nodeType == NodeType.Room)
		{
			roomCounter++;
		}
	}

	private void SetupNeighbors(IEnumerable<Vector3Int> positions)
	{
		foreach (Vector3Int position in positions)
		{
			SetupNeighbors(metaDataLayer.Get(position));
		}
	}

	private void SetupNeighbors(MetaDataNode node)
	{
		// Look in every direction for neighboring tiles.
		foreach (Vector3Int dir in MetaUtils.Directions)
		{
			Vector3Int neighbor = dir + node.Position;

			if (metaTileMap.IsSpaceAt(neighbor, true))
			{
				// // if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
				// if (node.IsRoom && !externalNodes.ContainsKey(node) && metaTileMap.IsSpaceAt(node.Position, true) == false)
				// {
				// 	externalNodes[node] = node;
				// }

				// If the node is not space, check other matrices if it has a tile next to this node.
				if (!node.IsSpace)
				{
					Vector3 neighborWorldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

					// if matrixManager says, it's not space at the neighboring position, there must be a matrix with a non-space tile
					if (!MatrixManager.IsSpaceAt(neighborWorldPosition.RoundToInt(), true))
					{
						MatrixInfo matrixInfo = MatrixManager.AtPoint(neighborWorldPosition.RoundToInt(), true);

						// ignore tilemap of current node
						if (matrixInfo != null && matrixInfo.MetaTileMap != metaTileMap)
						{
							// Check if atmos can pass to the neighboring position
							Vector3Int neighborlocalPosition = MatrixManager.WorldToLocalInt(neighborWorldPosition, matrixInfo);

							// add node of other matrix to the neighbors of the current node
							node.AddNeighbor(matrixInfo.MetaDataLayer.Get(neighborlocalPosition), dir);

							// skip other checks for neighboring tile on local tilemap, to prevent the space tile to be added as a neighbor
							continue;
						}
					}
				}
			}

			MetaDataNode neighborNode = metaDataLayer.Get(neighbor);

			if (metaTileMap.IsSpaceAt(neighbor, true))
			{
				neighborNode.Type = NodeType.Space;
			}

			if (neighborNode.Type == NodeType.Space)
			{
				neighborNode.ThermalConductivity = 0.4f;
				neighborNode.HeatCapacity =  700000f;
			}
			else if (neighborNode.Type == NodeType.Room)
			{
				neighborNode.ThermalConductivity = 0.04f;
				neighborNode.HeatCapacity =  10000f;
			}

			node.AddNeighbor(neighborNode, dir);
		}
	}

	private void ServerUpdateMe()
	{
		foreach (MetaDataNode node in externalNodes.Keys)
		{
			subsystemManager.UpdateAt(node.Position);
		}
	}
}

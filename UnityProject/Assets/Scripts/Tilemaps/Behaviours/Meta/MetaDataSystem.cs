using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Directionals;
using Systems.Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using Mirror;
using Tiles;

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



		if (MatrixManager.AtPoint(node.Position.ToWorld(node.PositionMatrix).RoundToInt(),
			    CustomNetworkManager.IsServer) == node.PositionMatrix.MatrixInfo)
		{
			MetaUtils.AddToNeighbors(node);
		}
	}

	private void LocateRooms()
	{
		var bounds = metaTileMap.GetLocalBounds();

		var watch = new Stopwatch();
		watch.Start();
		foreach (Vector3Int position in bounds.allPositionsWithin())
		{
			FindRoomAt(position);
		}
		Logger.LogFormat("Created rooms in {0}ms", Category.TileMaps, watch.ElapsedMilliseconds);
	}

	private void FindRoomAt(Vector3Int position)
	{
		//First try for full tile atmos blocks, e.g walls, closed doors and set them to occupied
		if (metaTileMap.IsAtmosPassableAt(position, true) == false)
		{
			MetaDataNode node = metaDataLayer.Get(position);
			node.Type = NodeType.Occupied;

			//Full doors, not windoors
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
		//Then we try to find the room at this position, if its not space or already in a room
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
		var roomPositions = new Dictionary<Vector3Int, NodeOccupiedType>();
		var freePositions = new UniqueQueue<Vector3Int>();

		freePositions.Enqueue(origin);

		var isSpace = false;

		// Breadth-first search of the connected tiles that are not occupied
		while (freePositions.IsEmpty == false)
		{
			if (freePositions.TryDequeue(out Vector3Int position))
			{
				roomPositions.Add(position, NodeOccupiedType.None);

				Vector3Int[] neighbors = MetaUtils.GetNeighbors(position, null);
				if(neighbors.Length == 0) continue;

				var freePositionDoors = matrix.GetAs<RegisterDoor>(position, true).ToArray();
				var freePositionDirectionalPassable = matrix.GetAs<DirectionalPassable>(position, true).ToArray();

				for (var i = 0; i < neighbors.Length; i++)
				{
					Vector3Int neighbor = neighbors[i];

					//If this position is space, test to see if whole room should be space
					if (metaTileMap.IsSpaceAt(neighbor, true))
					{
						Vector3Int worldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

						// If matrix manager says, the neighboring positions is space, the whole room is connected to space.
						// Otherwise there is another matrix, blocking off the connection to space.
						if (MatrixManager.IsSpaceAt(worldPosition, true, matrix.MatrixInfo))
						{
							isSpace = true;
						}

						continue;
					}

					//Now check to see if it is atmos passable between the position and its neighbour
					//If it is then its in the same room
					if (metaTileMap.IsAtmosPassableAt(position, neighbor, true))
					{
						// if neighbor position is not yet a room in the meta data layer and not in the room positions list,
						// add it to the positions that need be checked
						if (!roomPositions.ContainsKey(neighbor) && !metaDataLayer.IsRoomAt(neighbor))
						{
							freePositions.Enqueue(neighbor);
						}

						continue;
					}

					//If it isnt atmos passable between positions then it might be a windoor or directional window!

					//Check windoor
					for (int j = 0; j < freePositionDoors.Length; j++)
					{
						var windoor = freePositionDoors[j];

						//Check to see if windoor
						if(windoor.OneDirectionRestricted == false) continue;
						if(windoor.DirectionCheck(position, true, out var directionEnum) == false) continue;

						if (roomPositions.TryGetValue(position, out var occupiedType) == false)
						{
							Logger.LogError("Failed to find room position in dict, this shouldn't happen!!");
							continue;
						}

						var newType = occupiedType;
						if (newType == NodeOccupiedType.None)
						{
							newType = NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
						}
						else
						{
							newType |= NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
						}

						//Enum value type? have to set it back to dict?
						roomPositions[position] = newType;
					}

					//Check other DirectionalPassables
					for (int j = 0; j < freePositionDirectionalPassable.Length; j++)
					{
						var directionalPassable = freePositionDirectionalPassable[j];
						if(directionalPassable.IsAtmosPassableOnAll || directionalPassable.AtmosPassable) continue;
						if(directionalPassable.IsPassableAtSide(position, true, out var directionEnum) == false) continue;

						if (roomPositions.TryGetValue(position, out var occupiedType) == false)
						{
							Logger.LogError("Failed to find room position in dict, this shouldn't happen!!");
							continue;
						}

						var newType = occupiedType;
						if (newType == NodeOccupiedType.None)
						{
							newType = NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
						}
						else
						{
							newType |= NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
						}

						//Enum value type? have to set it back to dict?
						roomPositions[position] = newType;
					}
				}
			}
		}

		AssignType(roomPositions, isSpace ? NodeType.Space : NodeType.Room);

		SetupNeighbors(roomPositions);
	}

	private void AssignType(Dictionary<Vector3Int, NodeOccupiedType> positions, NodeType nodeType)
	{
		// Bulk assign type to nodes at given positions
		foreach (var position in positions)
		{
			MetaDataNode node = metaDataLayer.Get(position.Key);

			node.Type = nodeType;
			node.OccupiedType = position.Value;

			// assign room number, if type is room
			node.RoomNumber = nodeType == NodeType.Room ? roomCounter : -1;
		}

		// increase room counter, so next room will get a new number
		if (nodeType == NodeType.Room)
		{
			roomCounter++;
		}
	}

	private void SetupNeighbors(Dictionary<Vector3Int, NodeOccupiedType> positions)
	{
		foreach (var position in positions)
		{
			SetupNeighbors(metaDataLayer.Get(position.Key));
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
				// if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
				if (node.IsRoom && !externalNodes.ContainsKey(node) && metaTileMap.IsSpaceAt(node.Position, true) == false)
				{
					externalNodes[node] = node;
				}

				// If the node is not space, check other matrices if it has a tile next to this node.
				if (!node.IsSpace)
				{
					Vector3 neighborWorldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

					// if matrixManager says, it's not space at the neighboring position, there must be a matrix with a non-space tile
					if (!MatrixManager.IsSpaceAt(neighborWorldPosition.RoundToInt(), true, matrix.MatrixInfo))
					{
						MatrixInfo matrixInfo = MatrixManager.AtPoint(neighborWorldPosition.RoundToInt(), true);

						// ignore tilemap of current node
						if (matrixInfo != null && matrixInfo.MetaTileMap != metaTileMap)
						{
							// Check if atmos can pass to the neighboring position
							Vector3Int neighborlocalPosition = MatrixManager.WorldToLocalInt(neighborWorldPosition, matrixInfo);

							var OppositeNode = matrixInfo.MetaDataLayer.Get(neighborlocalPosition);

							// add node of other matrix to the neighbors of the current node
							node.AddNeighbor(OppositeNode, dir);

							if (dir == Vector3Int.up)
							{
								OppositeNode.AddNeighbor(node, Vector3Int.down);
							}
							else if (dir == Vector3Int.down)
							{
								OppositeNode.AddNeighbor(node, Vector3Int.up);
							}
							else if (dir == Vector3Int.right)
							{
								OppositeNode.AddNeighbor(node, Vector3Int.left);
							}
							else if (dir == Vector3Int.left)
							{
								OppositeNode.AddNeighbor(node, Vector3Int.right);
							}

							// if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
							if (OppositeNode.IsRoom && !OppositeNode.MetaDataSystem.externalNodes.ContainsKey(node) && OppositeNode.MetaDataSystem.metaTileMap.IsSpaceAt(OppositeNode.Position, true) == false)
							{
								OppositeNode.MetaDataSystem.externalNodes[OppositeNode] = OppositeNode;
							}

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
		if (matrix.MatrixMove != null && matrix.MatrixMove.IsMovingServer)
		{
			foreach (MetaDataNode node in externalNodes.Keys)
			{
				subsystemManager.UpdateAt(node.Position);
			}
		}
	}
}

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Core.Directionals;
using Logs;
using Systems.Atmospherics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;
using Mirror;
using Tiles;

/// <summary>
/// Subsystem behavior which manages updating the MetaDataNodes and simulation that affects them for a given matrix.
/// </summary>
public class MetaDataSystem : MatrixSystemBehaviour
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

	private AtmosSystem atmosSystem;

	private int roomCounter = 0;

	private HashSet<Vector3Int> tested;
	private bool setUpDone;
	public bool SetUpDone => setUpDone;

	public override void Awake()
	{
		base.Awake();
		matrix = GetComponentInChildren<Matrix>(true);
		externalNodes = new ConcurrentDictionary<MetaDataNode, MetaDataNode>();
		atmosSystem = GetComponent<AtmosSystem>();
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
			StartCoroutine(LocateRooms());
			Stopwatch Dsw = new Stopwatch();
			Dsw.Start();
			matrix.MetaTileMap.InitialiseUnderFloorUtilities(CustomNetworkManager.IsServer);
			Dsw.Stop();
			Loggy.Log($"Initialise {gameObject.name} Utilities (Power cables, Atmos pipes): " + Dsw.ElapsedMilliseconds + " ms", Category.Matrix);
		}

		sw.Stop();
		Loggy.Log($"{gameObject.name} MetaData init: " + sw.ElapsedMilliseconds + " ms", Category.Matrix);
	}

	public override void UpdateAt(Vector3Int localPosition)
	{
		MetaDataNode node = metaDataLayer.Get(localPosition);

		MetaUtils.RemoveFromNeighbors(node);
		externalNodes.TryRemove(node, out MetaDataNode nothing);

		node.OccupiedType = NodeOccupiedType.None;

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
			else
			{
				node.OccupiedType = DetectOccupiedType(localPosition);
			}
		}
		//Then must be fully blocked e.g wall or closed door
		else
		{
			node.Type = NodeType.Occupied;
			if (matrix.GetFirst<RegisterDoor>(localPosition, true))
			{
				node.OccupiedType = NodeOccupiedType.Full;

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

		var atPoint = MatrixManager.AtPoint(node.WorldPosition, CustomNetworkManager.IsServer);

		if (atPoint == MatrixManager.Instance.spaceMatrix.MatrixInfo)
		{
			externalNodes[node] = node;
		}

		if (atPoint == node.PositionMatrix.MatrixInfo || atPoint == MatrixManager.Instance.spaceMatrix.MatrixInfo)
		{
			MetaUtils.AddToNeighbors(node);
		}
	}

	private IEnumerator LocateRooms()
	{
		var bounds = metaTileMap.GetLocalBounds();

		var overallWatch = new Stopwatch();
		var frameWatch = new Stopwatch();
		overallWatch.Start();

		var count = 0;
		var matrixName = gameObject.name;
		var positions = bounds.allPositionsWithin();
		tested = new HashSet<Vector3Int>(positions.Count);

		Loggy.LogFormat($"{matrixName}: {positions.Count} tiles need to be set up for atmos.", Category.TileMaps);

		frameWatch.Start();

		foreach (Vector3Int position in positions)
		{
			if(tested.Contains(position)) continue;
			count++;

			//Every 1000 tiles wait till next frame to continue
			if (count % 1000 == 0)
			{
				Loggy.LogFormat($"{matrixName}: Created some rooms in {frameWatch.ElapsedMilliseconds}ms", Category.TileMaps);

				frameWatch.Reset();
				yield return WaitFor.EndOfFrame;
				frameWatch.Restart();
			}

			FindRoomAt(position);
		}

		setUpDone = true;

		Loggy.LogFormat($"{matrixName}: Created rooms in a total of {overallWatch.ElapsedMilliseconds}ms", Category.TileMaps);
		overallWatch.Reset();
		overallWatch.Restart();

		atmosSystem.FillRoomGas();

		Loggy.LogFormat($"{matrixName}: Filled rooms with gas in {overallWatch.ElapsedMilliseconds}ms", Category.TileMaps);
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
				node.OccupiedType = NodeOccupiedType.Full;
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
			if (freePositions.TryDequeue(out Vector3Int position) == false) continue;

			roomPositions.Add(position, DetectOccupiedType(position));

			Vector3Int[] neighbors = MetaUtils.GetNeighbors(position, null);
			if(neighbors.Length == 0) continue;

			for (var i = 0; i < neighbors.Length; i++)
			{
				Vector3Int neighbor = neighbors[i];

				//If this position is space on our matrix, test to see if space on other matrixes to work out if room should be space
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

				//Now check to see if it is atmos passable between the free position and its neighbour
				//If it is then its in the same room
				if (metaTileMap.IsAtmosPassableAt(position, neighbor, true))
				{
					// if neighbor position is not yet a room in the meta data layer and not in the room positions list,
					// add it to the positions that need be checked
					if (!roomPositions.ContainsKey(neighbor) && !metaDataLayer.IsRoomAt(neighbor))
					{
						freePositions.Enqueue(neighbor);
					}
				}
			}
		}

		AssignType(roomPositions, isSpace ? NodeType.Space : NodeType.Room);

		SetupNeighbors(roomPositions);
	}

	/// <summary>
	/// Might have a windoor or DirectionalPassables (e.g directional window)
	/// See if we need to set NodeOccupiedType so we block atmos from doing gas exchange in that direction
	/// </summary>
	private NodeOccupiedType DetectOccupiedType(Vector3Int position)
	{
		var freePositionDoors = matrix.GetAs<RegisterDoor>(position, true).ToArray();
		var freePositionDirectionalPassable = matrix.GetAs<DirectionalPassable>(position, true).ToArray();

		var occupiedType = NodeOccupiedType.None;

		//Check windoor
		for (int i = 0; i < freePositionDoors.Length; i++)
		{
			var windoor = freePositionDoors[i];

			//See if its has just been despawned as the UpdateAt has been called during IServerDespawn
			if (windoor.Active == false) continue;

			//Check to see if windoor
			if (windoor.OneDirectionRestricted == false || windoor.IsClosed == false) continue;
			if (windoor.RotatableChecked.HasComponent == false) continue;

			var directionEnum = windoor.RotatableChecked.Component.CurrentDirection;

			if (occupiedType == NodeOccupiedType.None)
			{
				occupiedType = NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
			}
			else
			{
				occupiedType |= NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
			}
		}

		//Check other DirectionalPassables
		for (int i = 0; i < freePositionDirectionalPassable.Length; i++)
		{
			var directionalPassable = freePositionDirectionalPassable[i];

			//See if its has just been despawned as the UpdateAt has been called during IServerDespawn
			if (directionalPassable.Active == false) continue;

			if (directionalPassable.IsAtmosPassableOnAll) continue;

			//Only allow atmos to be blocked by anchored objects
			if(directionalPassable.ObjectPhysics.HasComponent
			   && directionalPassable.ObjectPhysics.Component.isNotPushable == false) continue;

			var blockedOrientations = directionalPassable.GetOrientationsBlocked(directionalPassable.AtmosphericPassableSides);

			foreach (var directionEnum in blockedOrientations)
			{
				if (occupiedType == NodeOccupiedType.None)
				{
					occupiedType = NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
				}
				else
				{
					occupiedType |= NodeOccupiedUtil.DirectionEnumToOccupied(directionEnum);
				}
			}
		}

		return occupiedType;
	}

	private void AssignType(Dictionary<Vector3Int, NodeOccupiedType> positions, NodeType nodeType)
	{
		// Bulk assign type to nodes at given positions
		foreach (var position in positions)
		{
			MetaDataNode node = metaDataLayer.Get(position.Key);
			if(setUpDone == false) tested.Add(position.Key);

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
			Vector3Int neighbor = dir + node.LocalPosition;

			if (metaTileMap.IsSpaceAt(neighbor, true))
			{
				// if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
				if (node.IsRoom && !externalNodes.ContainsKey(node) && metaTileMap.IsSpaceAt(node.LocalPosition, true) == false)
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

							var oppositeNode = matrixInfo.MetaDataLayer.Get(neighborlocalPosition);

							// add node of other matrix to the neighbors of the current node
							node.AddNeighbor(oppositeNode, dir);

							if (dir == Vector3Int.up)
							{
								oppositeNode.AddNeighbor(node, Vector3Int.down);
							}
							else if (dir == Vector3Int.down)
							{
								oppositeNode.AddNeighbor(node, Vector3Int.up);
							}
							else if (dir == Vector3Int.right)
							{
								oppositeNode.AddNeighbor(node, Vector3Int.left);
							}
							else if (dir == Vector3Int.left)
							{
								oppositeNode.AddNeighbor(node, Vector3Int.right);
							}

							// if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
							if (oppositeNode.IsRoom && !oppositeNode.MetaDataSystem.externalNodes.ContainsKey(node) && oppositeNode.MetaDataSystem.metaTileMap.IsSpaceAt(oppositeNode.LocalPosition, true) == false)
							{
								oppositeNode.MetaDataSystem.externalNodes[oppositeNode] = oppositeNode;
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
				subsystemManager.UpdateAt(node.LocalPosition);
			}
		}
	}
}

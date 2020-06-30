using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

/// <summary>
/// Subsystem behavior which manages updating the MetaDataNodes and simulation that affects them for a given matrix.
/// </summary>
public class MetaDataSystem : SubsystemBehaviour
{
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

	void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public override void Initialize()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		if (MatrixManager.IsInitialized)
		{
			LocateRooms();
			Stopwatch Dsw = new Stopwatch();
			Dsw.Start();
			matrix.UnderFloorLayer.InitialiseUnderFloorUtilities();
			Dsw.Stop();
			Logger.Log("Initialise Station Utilities (Power cables, Atmos pipes): " + Dsw.ElapsedMilliseconds + " ms", Category.Matrix);
		}

		sw.Stop();

		Logger.Log("MetaData init: " + sw.ElapsedMilliseconds + " ms", Category.Matrix);
	}

	public override void UpdateAt(Vector3Int localPosition)
	{
		MetaDataNode node = metaDataLayer.Get(localPosition);

		MetaUtils.RemoveFromNeighbors(node);
		externalNodes.TryRemove(node, out MetaDataNode nothing);

		node.IsClosedAirlock = false;

		// If the node is atmos passable (i.e. space or room), we need to setup its neighbors again, otherwise it's occupied and does need a neighbor check
		if (metaTileMap.IsAtmosPassableAt(localPosition, true))
		{
			node.ClearNeighbors();
			node.Type = metaTileMap.IsSpaceAt(localPosition, true) ? NodeType.Space : NodeType.Room;
			SetupNeighbors(node);
			MetaUtils.AddToNeighbors(node);
		}
		else
		{
			node.Type = NodeType.Occupied;
			if (matrix.GetFirst<RegisterDoor>(localPosition, true))
			{
				node.IsClosedAirlock = true;
			}
		}

	}

	private void LocateRooms()
	{
		BoundsInt bounds = metaTileMap.GetBounds();

		foreach (Vector3Int position in bounds.allPositionsWithin)
		{
			FindRoomAt(position);
		}
	}


	private void FindRoomAt(Vector3Int position)
	{
		if (!metaTileMap.IsAtmosPassableAt(position, true))
		{
			MetaDataNode node = metaDataLayer.Get(position);
			node.Type = NodeType.Occupied;

			if (matrix.GetFirst<RegisterDoor>(position, true))
			{
				node.IsClosedAirlock = true;
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
		Vector3 nodeWorldPosition = MatrixManager.LocalToWorldInt(node.Position, MatrixManager.Get(matrix.Id));

		// Look in every direction for neighboring tiles.
		foreach (Vector3Int dir in MetaUtils.Directions)
		{
			Vector3Int neighbor = dir + node.Position;

			if (metaTileMap.IsSpaceAt(neighbor, true))
			{
				/*// if current node is a room, but the neighboring is a space tile, this node needs to be checked regularly for changes by other matrices
				if (node.IsRoom && !externalNodes.ContainsKey(node) && metaTileMap.IsSpaceAt(node.Position, true) == false)
				{
					externalNodes[node] = node;
				}*/

				// If the node is not space, check other matrices if it has a tile next to this node.
				if (!node.IsSpace)
				{
					Vector3 neighborWorldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

					// if matrixManager says, it's not space at the neighboring position, there must be a matrix with a non-space tile
					if (!MatrixManager.IsSpaceAt(neighborWorldPosition.RoundToInt(), true))
					{
						MatrixInfo matrixInfo = MatrixManager.AtPoint(neighborWorldPosition.RoundToInt(), true);

						// ignore tilemap of current node
						if (matrixInfo.MetaTileMap != metaTileMap)
						{
							// Check if atmos can pass to the neighboring position
							Vector3Int neighborlocalPosition = MatrixManager.WorldToLocalInt(neighborWorldPosition, matrixInfo);
							Vector3Int nodeLocalPosition = MatrixManager.WorldToLocalInt(nodeWorldPosition, matrixInfo);

							if (matrixInfo.MetaTileMap.IsAtmosPassableAt(nodeLocalPosition, neighborlocalPosition, true))
							{
								// add node of other matrix to the neighbors of the current node
								node.AddNeighbor(matrixInfo.MetaDataLayer.Get(neighborlocalPosition), dir);
							}

							// skip other checks for neighboring tile on local tilemap, to prevent the space tile to be added as a neighbor
							continue;
						}
					}
				}
			}

			// If neighboring tile on local tilemap is atmos passable, add it as a neighbor
			if (metaTileMap.IsAtmosPassableAt(node.Position, neighbor, true))
			{
				MetaDataNode neighborNode = metaDataLayer.Get(neighbor);

				if (metaTileMap.IsSpaceAt(neighbor, true))
				{
					neighborNode.Type = NodeType.Space;
				}

				node.AddNeighbor(neighborNode, dir);
			}
		}


	}

	void UpdateMe()
	{
		if (CustomNetworkManager.Instance._isServer == false) return;

		foreach (MetaDataNode node in externalNodes.Keys)
		{
			subsystemManager.UpdateAt(node.Position);
		}
	}
}
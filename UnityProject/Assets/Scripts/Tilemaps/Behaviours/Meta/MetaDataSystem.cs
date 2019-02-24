using System.Collections.Generic;
using System.Diagnostics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

/// <summary>
/// Subsystem behavior which manages updating the MetaDataNodes and simulation that affects them for a given matrix.
/// </summary>
public class MetaDataSystem : SubsystemBehaviour
{
	public override int Priority => 100;

	/// <summary>
	/// Nodes which exist in space next to room tiles of the matrix.
	/// </summary>
	private HashSet<MetaDataNode> externalNodes;
	/// <summary>
	/// Matrix this system is managing the MetaDataNodes for.
	/// </summary>
	private Matrix matrix;

	private int roomCounter = 0;

	// Set higher priority to ensure that it is executed before other systems

	public override void Awake()
	{
		base.Awake();

		matrix = GetComponentInChildren<Matrix>(true);
		externalNodes = new HashSet<MetaDataNode>();
	}

	void OnEnable()
	{
		UpdateManager.Instance.Add(UpdateMe);
	}

	void OnDisable()
	{
		if (UpdateManager.Instance != null)
		{
			UpdateManager.Instance.Remove(UpdateMe);
		}
	}

	public override void Initialize()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		if (MatrixManager.IsInitialized)
		{
			LocateRooms();
		}

		sw.Stop();

		Logger.Log("MetaData init: " + sw.ElapsedMilliseconds + " ms", Category.Matrix);
	}

	public override void UpdateAt(Vector3Int position)
	{
		MetaDataNode node = metaDataLayer.Get(position);

		MetaUtils.RemoveFromNeighbors(node);

		if (metaTileMap.IsAtmosPassableAt(position))
		{
			node.ClearNeighbors();

			SetupNeighbors(node);
			MetaUtils.AddToNeighbors(node);

			node.Type = metaTileMap.IsSpaceAt(position) ? NodeType.Space : NodeType.Room;
		}
		else
		{
			node.Type = NodeType.Occupied;
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
		if (!metaTileMap.IsAtmosPassableAt(position))
		{
			MetaDataNode node = metaDataLayer.Get(position);
			node.Type = NodeType.Occupied;

			SetupNeighbors(node);
		}
		else if (!metaTileMap.IsSpaceAt(position) && !metaDataLayer.IsRoomAt(position) && !metaDataLayer.IsSpaceAt(position))
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

		while (!freePositions.IsEmpty)
		{
			if (freePositions.TryDequeue(out Vector3Int position))
			{
				roomPositions.Add(position);

				Vector3Int[] neighbors = MetaUtils.GetNeighbors(position);
				for (var i = 0; i < neighbors.Length; i++)
				{
					Vector3Int neighbor = neighbors[i];
					if (metaTileMap.IsSpaceAt(neighbor))
					{
						Vector3Int worldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

						if (MatrixManager.IsSpaceAt(worldPosition))
						{
							isSpace = true;
						}
					}
					else if (metaTileMap.IsAtmosPassableAt(position, neighbor))
					{
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
		foreach (Vector3Int position in positions)
		{
			MetaDataNode node = metaDataLayer.Get(position);

			node.Type = nodeType;

			node.RoomNumber = nodeType == NodeType.Room ? roomCounter : -1;
		}

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
//		Vector3Int[] neighbors = MetaUtils.GetNeighbors(node.Position);

		Vector3 nodeWorldPosition = MatrixManager.LocalToWorldInt(node.Position, MatrixManager.Get(matrix.Id));

		foreach (Vector3Int dir in MetaUtils.Directions)
		{
			Vector3Int neighbor = dir + node.Position;

			if (metaTileMap.IsSpaceAt(neighbor))
			{
				if (node.IsRoom)
				{
					externalNodes.Add(node);
				}

				Vector3 neighborWorldPosition = MatrixManager.LocalToWorldInt(neighbor, MatrixManager.Get(matrix.Id));

				if (!MatrixManager.IsSpaceAt(neighborWorldPosition.RoundToInt()))
				{
					MatrixInfo matrixInfo = MatrixManager.AtPoint(neighborWorldPosition.RoundToInt());

					if (matrixInfo.MetaTileMap != metaTileMap)
					{
						Vector3Int neighborlocalPosition = MatrixManager.WorldToLocalInt(neighborWorldPosition, matrixInfo);
						Vector3Int nodeLocalPosition = MatrixManager.WorldToLocalInt(nodeWorldPosition, matrixInfo);

						if (matrixInfo.MetaTileMap.IsAtmosPassableAt(nodeLocalPosition, neighborlocalPosition))
						{
							node.AddNeighbor(matrixInfo.MetaDataLayer.Get(neighborlocalPosition));
						}

						continue;
					}
				}
			}

			if (metaTileMap.IsAtmosPassableAt(node.Position, neighbor))
			{
				MetaDataNode neighborNode = metaDataLayer.Get(neighbor);

				if (metaTileMap.IsSpaceAt(neighbor))
				{
					neighborNode.Type = NodeType.Space;
				}

				node.AddNeighbor(neighborNode);
			}
		}
	}

	void UpdateMe()
	{
		foreach (MetaDataNode node in externalNodes)
		{
			subsystemManager.UpdateAt(node.Position);
		}
	}
}
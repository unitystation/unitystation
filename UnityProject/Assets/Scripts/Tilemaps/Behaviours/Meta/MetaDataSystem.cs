using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

public class MetaDataSystem : SubsystemBehaviour
{
	private HashSet<MetaDataNode> externalNodes;

	// Set higher priority to ensure that it is executed before other systems
	public override int Priority => 100;

	public override void Awake()
	{
		base.Awake();

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

		if (metaTileMap.IsAtmosPassableAt(position))
		{
			MetaUtils.RemoveFromNeighbors(node);
			node.ClearNeighbors();

			SetupNeighbors(node);
			MetaUtils.AddToNeighbors(node);

			node.Type = metaTileMap.IsSpaceAt(position) ? NodeType.Space : NodeType.Room;
		}
		else
		{
			node.Type = NodeType.Occupied;
			MetaUtils.RemoveFromNeighbors(node);
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
			Vector3Int position;
			if (freePositions.TryDequeue(out position))
			{
				roomPositions.Add(position);

				foreach (Vector3Int neighbor in MetaUtils.GetNeighbors(position))
				{
					if (metaTileMap.IsSpaceAt(neighbor))
					{
						Vector3 worldPosition = transform.TransformPoint(neighbor);
						if (MatrixManager.IsSpaceAt(worldPosition.RoundToInt()))
						{
							isSpace = true;
						}
					}
					else if (metaDataLayer.IsSpaceAt(neighbor))
					{
						isSpace = true;
					}
					else if (metaTileMap.IsAtmosPassableAt(neighbor))
					{
						if (!roomPositions.Contains(neighbor) && !freePositions.Contains(neighbor) && !metaDataLayer.IsRoomAt(neighbor))
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
		node.tempCount++;

		foreach (Vector3Int neighbor in MetaUtils.GetNeighbors(node.Position))
		{
			if (metaTileMap.IsSpaceAt(neighbor))
			{
				if (node.IsRoom)
				{
					externalNodes.Add(node);
				}

				Vector3 worldPosition = transform.TransformPoint(neighbor) + Vector3.one * 0.5f;
				worldPosition.z = 0;
				if (!MatrixManager.IsSpaceAt(worldPosition.RoundToInt()))
				{
					MatrixInfo matrixInfo = MatrixManager.AtPoint(worldPosition.RoundToInt());

					Vector3Int localPosition = MatrixManager.WorldToLocalInt(worldPosition, matrixInfo);

					if (matrixInfo.MetaTileMap.IsAtmosPassableAt(localPosition))
					{
						node.AddNeighbor(matrixInfo.MetaDataLayer.Get(localPosition));
					}

					continue;
				}
			}

			if (metaTileMap.IsAtmosPassableAt(neighbor))
			{
				node.AddNeighbor(metaDataLayer.Get(neighbor));
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
using System.Collections.Generic;
using System.Diagnostics;
using Tilemaps.Behaviours.Meta;
using UnityEngine;

[ExecuteInEditMode]
public class RoomControl : SystemBehaviour
{
	// Set higher priority to ensure that it is executed before other systems
	public override int Priority => 100;

	public override void Initialize()
	{
		Stopwatch sw = new Stopwatch();
		sw.Start();

		LocateRooms();

		sw.Stop();

		Logger.Log("Room init: " + sw.ElapsedMilliseconds + " ms", Category.Atmos);
	}

	public override void UpdateAt(Vector3Int position)
	{
		MetaDataNode node = metaDataLayer.Get(position);

		if (metaTileMap.IsAtmosPassableAt(position))
		{
			node.ClearNeighbors();
			SetupNeighbors(position);
			MetaUtils.AddToNeighbors(node);

			if (metaTileMap.IsSpaceAt(position))
			{
				node.Type = NodeType.Space;
			}
			else
			{
				node.Type = NodeType.Room;
			}
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
		if (Check(position) && !metaDataLayer.IsRoomAt(position))
		{
			CreateRoom(position);
		}
		else
		{
			if (!metaTileMap.IsAtmosPassableAt(position))
			{
				MetaDataNode node = metaDataLayer.Get(position);
				node.Type = NodeType.Occupied;

				SetupNeighbors(position);
			}
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
					if (Check(neighbor))
					{
						if (!roomPositions.Contains(neighbor) && !freePositions.Contains(neighbor) && !metaDataLayer.IsRoomAt(neighbor))
						{
							freePositions.Enqueue(neighbor);
						}
					}
					else if (metaTileMap.IsSpaceAt(neighbor))
					{
						Vector3 worldPosition = transform.TransformPoint(neighbor);
						if (MatrixManager.IsSpaceAt(worldPosition.RoundToInt()))
						{
							isSpace = true;
						}
						else
						{
							Logger.Log("");
						}
					}
				}
			}
		}

		if (!isSpace)
		{
			AssignRoom(roomPositions);
		}

		SetupNeighbors(roomPositions);
	}

	private void AssignRoom(IEnumerable<Vector3Int> positions)
	{
		foreach (Vector3Int position in positions)
		{
			MetaDataNode node = metaDataLayer.Get(position);

			node.Type = NodeType.Room;
		}
	}

	private bool Check(Vector3Int position)
	{
		return metaTileMap.IsAtmosPassableAt(position) && !metaTileMap.IsSpaceAt(position);
	}

	private void SetupNeighbors(IEnumerable<Vector3Int> positions)
	{
		foreach (Vector3Int position in positions)
		{
			SetupNeighbors(position);
		}
	}

	private void SetupNeighbors(Vector3Int position)
	{
		MetaDataNode node = metaDataLayer.Get(position);

		foreach (Vector3Int neighbor in MetaUtils.GetNeighbors(position))
		{
			if (metaTileMap.IsSpaceAt(neighbor))
			{
				Vector3 worldPosition = transform.TransformPoint(neighbor);
				if (!MatrixManager.IsSpaceAt(worldPosition.RoundToInt()))
				{
					Logger.Log("");
				}
			}

			if (metaTileMap.IsAtmosPassableAt(neighbor))
			{
				node.AddNeighbor(metaDataLayer.Get(neighbor));
			}
		}
	}
}
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Tilemaps.Behaviours.Meta
{
	[ExecuteInEditMode]
	public class RoomControl : SystemBehaviour
	{
		private static Vector3Int[] directions = {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right};

		// Set higher priority to ensure that it is executed before other systems
		public override int Priority => 100;

		public override void Initialize()
		{
			BoundsInt bounds = metaTileMap.GetBounds();

			int roomCounter = 0;

			Stopwatch sw = new Stopwatch();
			sw.Start();

			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				MetaDataNode node = metaDataLayer.Get(position, false);

				if (node.Room < 0 && !metaTileMap.IsSpaceAt(position))
				{
					if (metaTileMap.IsAtmosPassableAt(position))
					{
						if (FindRoom(position, roomCounter))
						{
							roomCounter++;
						}
					}
					else
					{
						node = metaDataLayer.Get(position);
						node.Type = NodeType.Wall;
					}
				}
			}

			sw.Stop();

			Debug.Log("Room init: " + sw.ElapsedMilliseconds + " ms");
		}

		public override void UpdateAt(Vector3Int position)
		{
			MetaDataNode node = metaDataLayer.Get(position);

			if (metaTileMap.IsSpaceAt(position))
			{
				node.Type = NodeType.Space;
			}
			else if (metaTileMap.IsAtmosPassableAt(position))
			{
				node.Room = 10000000;
				node.Type = NodeType.Room;
			}
			else
			{
				node.Type = NodeType.Wall;
			}
		}

		private MetaDataNode GetNodeAt(Vector3Int position)
		{
			MetaDataNode node = metaDataLayer.Get(position, false);
			
			if (metaTileMap.IsSpaceAt(position))
			{
				node.Type = NodeType.Space;
			}
			else if (metaTileMap.IsAtmosPassableAt(position))
			{
				foreach (Vector3Int dir in directions)
				{
					Vector3Int neighbor = position + dir;
					MetaDataNode neighborNode = GetNodeAt(neighbor);

					if (neighborNode.IsSpace)
					{
						node.Type = NodeType.Space;
						break;
					}
				}
			}
			else
			{
				node.Type = NodeType.Wall;
			}

			return node;
		}

		private bool FindRoom(Vector3Int position, int roomNumber)
		{
			Queue<Vector3Int> posToCheck = new Queue<Vector3Int>();
			HashSet<Vector3Int> roomPositions = new HashSet<Vector3Int>();

			posToCheck.Enqueue(position);

			bool isSpace = false;

			while (posToCheck.Count > 0)
			{
				Vector3Int pos = posToCheck.Dequeue();
				roomPositions.Add(pos);

				foreach (Vector3Int dir in directions)
				{
					Vector3Int neighbor = pos + dir;

					if (!posToCheck.Contains(neighbor) && !roomPositions.Contains(neighbor))
					{
						if (metaTileMap.IsSpaceAt(neighbor))
						{
							isSpace = true;
							MetaDataNode node = metaDataLayer.Get(neighbor);
							node.Type = NodeType.Space;
						}
						else if (metaTileMap.IsAtmosPassableAt(neighbor))
						{
							posToCheck.Enqueue(neighbor);
						}
						else
						{
							MetaDataNode node = metaDataLayer.Get(neighbor);
							node.Type = NodeType.Wall;
						}
					}
				}
			}

			foreach (Vector3Int p in roomPositions)
			{
				MetaDataNode node = metaDataLayer.Get(p);

				if (isSpace)
				{
					AtmosUtils.SetEmpty(node);
					node.Type = NodeType.Space;
				}
				else
				{
					AtmosUtils.SetAir(node);

					node.Type = NodeType.Room;
					node.Room = roomNumber;
				}
			}

			return !isSpace;
		}
	}
}
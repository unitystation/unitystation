﻿using System.Collections.Generic;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Meta
{
	[ExecuteInEditMode]
	public class RoomControl : SystemBehaviour
	{
		public override void Initialize()
		{
			BoundsInt bounds = metaTileMap.GetBounds();

			int roomCounter = 1;

			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				MetaDataNode node = metaDataLayer.Get(position, false);

				if ((node == null || node.Room == 0) && !metaTileMap.IsSpaceAt(position) && metaTileMap.IsAtmosPassableAt(position))
				{
					if (FindRoom(position, roomCounter))
					{
						roomCounter++;
					}
				}
			}

			sw.Stop();
			Logger.Log("Room init: " + sw.ElapsedMilliseconds + " ms",Category.RoomControl);
		}
		
		public override void UpdateAt(Vector3Int position)
		{
			MetaDataNode node = metaDataLayer.Get(position);
			if (metaTileMap.IsAtmosPassableAt(position))
			{
				node.Room = 10000000;
			}
			else
			{
				node.Room = 0;
			}
		}

		private bool FindRoom(Vector3Int position, int roomNumber)
		{
			Queue<Vector3Int> posToCheck = new Queue<Vector3Int>();
			HashSet<Vector3Int> roomPosition = new HashSet<Vector3Int>();

			posToCheck.Enqueue(position);

			bool isSpace = false;

			while (posToCheck.Count > 0)
			{
				Vector3Int pos = posToCheck.Dequeue();
				roomPosition.Add(pos);

				foreach (Vector3Int dir in new[] {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right})
				{
					Vector3Int neighbor = pos + dir;

					if (!posToCheck.Contains(neighbor) && !roomPosition.Contains(neighbor))
					{
						if (metaTileMap.IsSpaceAt(neighbor))
						{
							isSpace = true;
						}
						else if (metaTileMap.IsAtmosPassableAt(neighbor))
						{
							posToCheck.Enqueue(neighbor);
						}
					}
				}
			}

			foreach (Vector3Int p in roomPosition)
			{
				MetaDataNode node = metaDataLayer.Get(p);
				node.Room = isSpace ? -1 : roomNumber;
			}

			return !isSpace;
		}
	}
}
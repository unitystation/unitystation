﻿using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Meta.Data;
using UnityEngine;
using UnityEngine.Networking;

namespace Tilemaps.Behaviours.Meta
{
	[ExecuteInEditMode]
	public class RoomControl : SystemBehaviour
	{
		// Set higher priority to ensure that it is executed before other systems
		public override int Priority => 100;

		public override void Initialize()
		{
			BoundsInt bounds = metaTileMap.GetBounds();

			int roomCounter = 1;

			System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
			sw.Start();

			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				MetaDataNode node = metaDataLayer.Get(position, false);

				if (node.Room == 0 && !metaTileMap.IsSpaceAt(position) && metaTileMap.IsAtmosPassableAt(position))
				{
					if (FindRoom(position, roomCounter))
					{
						roomCounter++;
					}
				}
			}

			sw.Stop();

			Debug.Log("Room init: " + sw.ElapsedMilliseconds + " ms");
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
			HashSet<Vector3Int> roomPositions = new HashSet<Vector3Int>();

			posToCheck.Enqueue(position);

			bool isSpace = false;

			while (posToCheck.Count > 0)
			{
				Vector3Int pos = posToCheck.Dequeue();
				roomPositions.Add(pos);

				foreach (Vector3Int dir in new[] {Vector3Int.up, Vector3Int.left, Vector3Int.down, Vector3Int.right})
				{
					Vector3Int neighbor = pos + dir;

					if (!posToCheck.Contains(neighbor) && !roomPositions.Contains(neighbor))
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

			foreach (Vector3Int p in roomPositions)
			{
				MetaDataNode node = metaDataLayer.Get(p);

				if (isSpace)
				{
					node.Room = -1;
					node.Temperature = 2.7f;
					node.Moles = 0.000000316f;
				}
				else
				{
					node.Room = roomNumber;
					node.AirMix[Gas.Oxygen.Index] = 16.628484400890768491815384755837f;
					node.AirMix[Gas.Nitrogen.Index] = 66.513937603563073967261539023347f;
					node.Temperature = 293.15f;
					node.Moles = 83.142422004453842459076923779184f;
				}
			}

			return !isSpace;
		}
	}
}
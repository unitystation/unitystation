﻿using System.Collections.Generic;
using Objects;
using UnityEngine;

namespace Tilemaps.Utils
{
	/// <summary>
	/// Stores the RegisterTiles at each tile position.
	/// </summary>
	public class TileList
	{
		private readonly ChunkedTileMap<List<RegisterTile>> objects = new ChunkedTileMap<List<RegisterTile>>();

		private static readonly List<RegisterTile> EmptyList = new List<RegisterTile>();

		//permanent list to avoid the usage of ienumerators
		private readonly List<RegisterTile> tempRegisterTiles = new List<RegisterTile>();

		//20x20 = 400 tiles in chunk
		private const int ChunkSize = 20;

		public List<RegisterTile> AllObjects
		{
			get
			{
				var list = new List<RegisterTile>();

				foreach (var chunk in objects)
				{
					list.AddRange(chunk);
				}

				return list;
			}
		}

		public void Add(Vector3Int position, RegisterTile obj)
		{
			var Place = objects[position];
			if (Place == null)
			{
				Place = new List<RegisterTile>();
				objects[position] = Place;
			}
			Place.Add(obj);

			ReorderObjects(position);
		}

		public void Remove(Vector3Int position, RegisterTile obj)
		{
			if (objects.TryGetValue(position, out var objectsOut))
			{
				objectsOut.Remove(obj);
				//TODO Clearing out if there are 0 elements?
			}
		}

		public void ReorderObjects(Vector3Int position)
		{
			var offset = 0;
			if (position.x % 2 != 0)
			{
				offset = position.y % 2 != 0 ? 1 : 0;
			}
			else
			{
				offset = position.y % 2 != 0 ? 3 : 2;
			}

			var Objects = objects[position];

			var i = 0;
			foreach (var register in Objects)
			{
				register.SetNewSortingOrder((i * 4) + offset);
				i++;
			}
		}

		public bool HasObjects(Vector3Int localPosition)
		{
			return objects.TryGetValue(localPosition, out var chunk) && chunk != null && chunk.Count != 0;
		}

		public List<RegisterTile> Get(Vector3Int position)
		{
			return objects.TryGetValue(position, out var objectsOut) ? objectsOut : EmptyList;
		}

		public List<RegisterTile> Get(Vector3Int position, ObjectType type)
		{
			if (objects.TryGetValue(position, out var objectsOut) == false)
			{
				return EmptyList;
			}

			var list = new List<RegisterTile>();
			foreach (var x in objectsOut)
			{
				if (x.ObjectType == type)
				{
					list.Add(x);
				}
			}

			return list;
		}

		public void InvokeOnObjects(IRegisterTileAction action, Vector3Int localPosition)
		{
			tempRegisterTiles.Clear();
			tempRegisterTiles.AddRange(Get(localPosition));

			for (int i = tempRegisterTiles.Count - 1; i >= 0; i--)
			{
				if (tempRegisterTiles[i] != null) //explosions can delete many objects in this tile!
				{
					action.Invoke(tempRegisterTiles[i]);
				}
			}
		}
	}

	public class EnterTileBaseList
	{
		private readonly Dictionary<Vector3Int, TileListChunk<EnterTileBase>> objects = new Dictionary<Vector3Int, TileListChunk<EnterTileBase>>();

		private static readonly List<EnterTileBase> EmptyList = new List<EnterTileBase>();

		//20x20 = 400 tiles in chunk
		private const int ChunkSize = 20;

		public void Add(Vector3Int position, EnterTileBase obj)
		{
			var chunkPos = GetChunkPos(position);

			if (objects.ContainsKey(chunkPos) == false)
			{
				objects[chunkPos] = new TileListChunk<EnterTileBase>();
			}

			objects[chunkPos].Add(position, obj);
		}

		public void Remove(Vector3Int position, EnterTileBase obj = null)
		{
			var chunkPos = GetChunkPos(position);

			if (objects.TryGetValue(chunkPos, out var objectsOut))
			{
				objectsOut.Remove(position, obj);

				if (objectsOut.HasObjects() == false)
				{
					objects.Remove(chunkPos);
				}
			}
		}

		public List<EnterTileBase> Get(Vector3Int position)
		{
			var chunkPos = GetChunkPos(position);

			return objects.TryGetValue(chunkPos, out var objectsOut) ? objectsOut.Get(position) : EmptyList;
		}

		private Vector3Int GetChunkPos(Vector3Int tileLocalPos)
		{
			return new Vector3Int(tileLocalPos.x / ChunkSize, tileLocalPos.y / ChunkSize);
		}
	}
}
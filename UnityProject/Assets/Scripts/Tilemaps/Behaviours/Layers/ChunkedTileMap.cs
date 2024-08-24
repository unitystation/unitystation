using System;
using System.Collections;
using System.Collections.Generic;
using Logs;
using UnityEngine;


public class ChunkedTileMap<T> : IEnumerable<T> where T : class
{
	private const int ChunkSize = 32;

	private readonly int MaxChunkRangeBeforeDictionaryBackup = 64;

	private readonly int MaxChunkTileRange= 64;

	public List<List<T[,]>> chunksXY = new List<List<T[,]>>();
	public List<List<T[,]>> chunksXnY = new List<List<T[,]>>();
	public List<List<T[,]>> chunksnXnY = new List<List<T[,]>>();
	public List<List<T[,]>> chunksnXY = new List<List<T[,]>>();

	public Dictionary<Vector3Int, T> OverflowDictionary = new Dictionary<Vector3Int, T>();
	//This is for any silly values that happen to end up in here, so it doesn't consume all the RAM in the universe

	public ChunkedTileMap(int InMaxChunkRangeBeforeDictionaryBackup =  64  ) //64 * ChunkSize (32) = 2,048 Tiles
	{
		MaxChunkRangeBeforeDictionaryBackup = InMaxChunkRangeBeforeDictionaryBackup;
		MaxChunkTileRange = MaxChunkRangeBeforeDictionaryBackup * ChunkSize;
	}



	public bool ContainsKey(Vector3Int position)
	{
		return GetTile(position) != null;
	}

	public bool TryGetValue(Vector3Int position, out T Value, bool Expand = false)
	{
		Value = GetTile(position,Expand );
		return Value != null;
	}


	public T this[Vector3Int position]
	{
		get
		{
			return GetTile(position);
		}
		set
		{
			if (Mathf.Abs(position.x) > MaxChunkTileRange || Mathf.Abs(position.y) > MaxChunkTileRange)
			{
				OverflowDictionary[position] = value;
				return;
			}

			var Chunk = GetChunkAtAndIgnoreDictionaryRecords(position, true);
			int localX = Mathf.Abs(Mathf.FloorToInt(position.x) % ChunkSize);
			int localY = Mathf.Abs(Mathf.FloorToInt(position.y) % ChunkSize);
			try
			{
				Chunk[localX, localY] = value;
			}
			catch (Exception e)
			{
				Loggy.LogError(e.ToString());
			}

		}
	}
	private T[,] GetChunkFromList2D(List<List<T[,]>> List2D, Vector3Int position, bool Expand = false)
	{
		int chunkX = Mathf.Abs(Mathf.FloorToInt( (float)position.x / ChunkSize));
		int chunkY = Mathf.Abs(Mathf.FloorToInt((float)position.y / ChunkSize));

		if ((List2D.Count > chunkX) == false)
		{
			if (Expand)
			{
				// Calculate the number of elements to add to reach the specified index
				int elementsToAdd = chunkX + 1 - List2D.Count;

				// Add default values or your desired elements to the list
				for (int i = 0; i < elementsToAdd; i++)
				{
					List2D.Add(null);
				}
			}
			else
			{
				return null;
			}
		}

		var chunksX =List2D[chunkX];
		if (chunksX == null)
		{
			chunksX = new List<T[,]>();
			List2D[chunkX] = chunksX;
		}
		if ((chunksX.Count > chunkY) == false)
		{
			if (Expand)
			{
				// Calculate the number of elements to add to reach the specified index
				int elementsToAdd = chunkY + 1 - chunksX.Count;

				// Add default values or your desired elements to the list
				for (int i = 0; i < elementsToAdd; i++)
				{
					chunksX.Add(null); // You can change this to the default value or a specific value for your type
				}

				chunksX[chunkY] = new T[ChunkSize, ChunkSize];
			}
			else
			{
				return null;
			}
		}

		var Result = chunksX[chunkY];
		if (Expand)
		{
			if (Result == null)
			{
				Result = new T[ChunkSize, ChunkSize];
				chunksX[chunkY] = Result;
			}
		}

		return Result;
	}


	public T[,] GetChunkAtAndIgnoreDictionaryRecords(Vector3Int position, bool Expand = false)
	{

		if (position.x >= 0)
		{
			if (position.y >= 0)
			{
				return GetChunkFromList2D(chunksXY, position, Expand);
			}
			else
			{
				return GetChunkFromList2D(chunksXnY, position, Expand);
			}
		}
		else
		{
			if (position.y >= 0)
			{
				return GetChunkFromList2D(chunksnXY, position, Expand);
			}
			else
			{
				return GetChunkFromList2D(chunksnXnY, position, Expand);
			}
		}
	}



	public T GetTile(Vector3Int position, bool Expand = false)
	{
		if (Mathf.Abs(position.x) > MaxChunkTileRange || Mathf.Abs(position.y) > MaxChunkTileRange)
		{
			return OverflowDictionary.GetValueOrDefault(position);
		}

		var Chunk = GetChunkAtAndIgnoreDictionaryRecords(position, Expand);
		int localX = Mathf.Abs(Mathf.FloorToInt(position.x) % ChunkSize);
		int localY = Mathf.Abs(Mathf.FloorToInt(position.y) % ChunkSize);
		if (Chunk != null)
		{
			return Chunk[localX, localY];
		}
		else
		{
			return null;
		}
	}


// IEnumerable<T> implementation for foreach
	public IEnumerator<T> GetEnumerator()
	{
		foreach (var chunkList in chunksXY)
		{
			foreach (var chunk in chunkList)
			{
				if (chunk != null)
				{
					foreach (var tile in chunk)
					{
						if (tile != null)
						{
							yield return tile;
						}
					}
				}
			}
		}

		foreach (var chunkList in chunksXnY)
		{
			foreach (var chunk in chunkList)
			{
				if (chunk != null)
				{
					foreach (var tile in chunk)
					{
						if (tile != null)
						{
							yield return tile;
						}
					}
				}
			}
		}

		foreach (var chunkList in chunksnXnY)
		{
			foreach (var chunk in chunkList)
			{
				if (chunk != null)
				{
					foreach (var tile in chunk)
					{
						if (tile != null)
						{
							yield return tile;
						}
					}
				}
			}
		}

		foreach (var chunkList in chunksnXY)
		{
			foreach (var chunk in chunkList)
			{
				if (chunk != null)
				{
					foreach (var tile in chunk)
					{
						if (tile != null)
						{
							yield return tile;
						}
					}
				}
			}
		}

		foreach (var KV in OverflowDictionary)
		{
			yield return KV.Value;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		chunksXY.Clear();
		chunksXnY.Clear();
		chunksnXnY.Clear();
		chunksnXY.Clear();
	}
}
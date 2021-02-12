using System;
using UnityEngine;
using System.Collections.Concurrent;

/// <summary>
/// Using main thread to spawn prefabs into the world
/// </summary>
public static class SpawnSafeThread
{
	private static ConcurrentQueue<Tuple<Vector3, GameObject>> prefabsToSpawn = new ConcurrentQueue<Tuple<Vector3, GameObject>>();
	private static ConcurrentQueue<Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color>> tilesToUpdate = new ConcurrentQueue<Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color>>();

	public static void Process()
	{
		if (!prefabsToSpawn.IsEmpty)
		{
			Tuple<Vector3, GameObject> tuple;
			while (prefabsToSpawn.TryDequeue(out tuple))
			{
				Spawn.ServerPrefab(tuple.Item2, tuple.Item1, MatrixManager.GetDefaultParent(tuple.Item1, true));
			}
		}

		if (!tilesToUpdate.IsEmpty)
		{
			Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color> tuple;
			while (tilesToUpdate.TryDequeue(out tuple))
			{
				UpdateTileMessage.Send(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6);
			}
		}
	}

	public static void SpawnPrefab(Vector3 tilePos, GameObject prefabObject)
	{
		prefabsToSpawn.Enqueue(new Tuple<Vector3, GameObject>(tilePos, prefabObject));
	}

	public static void UpdateTileMessageSend(uint tileChangeManagerNetID, Vector3Int position, TileType tileType,
		string tileName, Matrix4x4 transformMatrix, Color colour)
	{
		tilesToUpdate.Enqueue(
			new Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color>
				(tileChangeManagerNetID, position, tileType, tileName, transformMatrix, colour));
	}
}
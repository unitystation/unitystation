using System;
using UnityEngine;
using System.Collections.Concurrent;

/// <summary>
/// Using main thread to spawn prefabs into the world
/// </summary>
public static class SpawnSafeThread
{
	private static ConcurrentQueue<Tuple<Vector3, GameObject>> prefabsToSpawn = new ConcurrentQueue<Tuple<Vector3, GameObject>>();

	public static void Process()
	{
		if (prefabsToSpawn.IsEmpty)
			return;

		Tuple<Vector3, GameObject> tuple;
		while (prefabsToSpawn.TryDequeue(out tuple))
		{
			Spawn.ServerPrefab(tuple.Item2, tuple.Item1, MatrixManager.GetDefaultParent(tuple.Item1, true));
		}
	}

	public static void SpawnPrefab(Vector3 tilePos, GameObject prefabObject)
	{
		prefabsToSpawn.Enqueue(new Tuple<Vector3, GameObject>(tilePos, prefabObject));
	}
}
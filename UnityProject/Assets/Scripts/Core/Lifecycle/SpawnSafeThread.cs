using System;
using UnityEngine;
using System.Collections.Concurrent;
using Messages.Server;

/// <summary>
/// Using main thread to spawn prefabs into the world
/// </summary>
public static class SpawnSafeThread
{
	private static ConcurrentQueue<SpawnSafeThreadData> prefabsToSpawn = new ConcurrentQueue<SpawnSafeThreadData>();

	public static void Process()
	{
		if (!prefabsToSpawn.IsEmpty)
		{
			SpawnSafeThreadData data;
			while (prefabsToSpawn.TryDequeue(out data))
			{
				var result = Spawn.ServerPrefab(data.Prefab, data.WorldPosition,
					data.ParentTransform == null
						? MatrixManager.GetDefaultParent(data.WorldPosition, true)
						: data.ParentTransform,
					count: data.Amount);

				//Greater than one as most items start stacked at 1
				if (result.Successful && data.AmountIfStackable > 1 &&
				    result.GameObject.TryGetComponent<Stackable>(out var stackable))
				{
					// -1 as we are adding, eg if we want 10 in stack, item already starts at 1 so we add 9
					stackable.ServerIncrease(data.AmountIfStackable - 1);
				}
			}
		}
	}

	public static void SpawnPrefab(Vector3 tilePos, GameObject prefabObject, Transform parentTransform = null,
		int amount = 1, int amountIfStackable = 0)
	{
		prefabsToSpawn.Enqueue(new SpawnSafeThreadData(tilePos, prefabObject, parentTransform, amount,
			amountIfStackable));
	}
}

public class SpawnSafeThreadData
{
	public Vector3 WorldPosition;
	public GameObject Prefab;

	public Transform ParentTransform;

	//Number of objects to spawn
	public int Amount;

	//If stackable how many in stack
	//Assumes item spawns with 1 in stack already
	public int AmountIfStackable;

	public SpawnSafeThreadData(Vector3 worldPosition, GameObject prefab, Transform parentTransform, int amount,
		int amountIfStackable)
	{
		WorldPosition = worldPosition;
		Prefab = prefab;
		ParentTransform = parentTransform;
		//Min 1
		Amount = amount < 1 ? 1 : amount;
		//Min 0
		AmountIfStackable = amountIfStackable < 0 ? 0 : amountIfStackable;
	}
}
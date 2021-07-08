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

	private static ConcurrentQueue<Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color, LayerType>> tilesToUpdate
		= new ConcurrentQueue<Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color, LayerType>>();

	private static ConcurrentQueue<Tuple<uint, Vector3Int, LayerType>> tilesToRemove
		= new ConcurrentQueue<Tuple<uint, Vector3Int, LayerType>>();

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

		if (!tilesToUpdate.IsEmpty)
		{
			Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color, LayerType> tuple;
			while (tilesToUpdate.TryDequeue(out tuple))
			{
				UpdateTileMessage.Send(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, tuple.Item5, tuple.Item6,
					tuple.Item7);
			}
		}

		if (!tilesToRemove.IsEmpty)
		{
			Tuple<uint, Vector3Int, LayerType> tuple;
			while (tilesToRemove.TryDequeue(out tuple))
			{
				RemoveTileMessage.Send(tuple.Item1, tuple.Item2, tuple.Item3);
			}
		}
	}

	public static void SpawnPrefab(Vector3 tilePos, GameObject prefabObject, Transform parentTransform = null,
		int amount = 1, int amountIfStackable = 0)
	{
		prefabsToSpawn.Enqueue(new SpawnSafeThreadData(tilePos, prefabObject, parentTransform, amount,
			amountIfStackable));
	}

	public static void UpdateTileMessageSend(uint matrixSyncNetID, Vector3Int position, TileType tileType,
		string tileName, Matrix4x4 transformMatrix, Color colour)
	{
		tilesToUpdate.Enqueue(
			new Tuple<uint, Vector3Int, TileType, string, Matrix4x4, Color, LayerType>
				(matrixSyncNetID, position, tileType, tileName, transformMatrix, colour, LayerType.None));
	}

	public static void RemoveTileMessageSend(uint matrixSyncNetID, Vector3Int cellPosition, LayerType layerType)
	{
		tilesToRemove.Enqueue(new Tuple<uint, Vector3Int, LayerType>(matrixSyncNetID, cellPosition, layerType));
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
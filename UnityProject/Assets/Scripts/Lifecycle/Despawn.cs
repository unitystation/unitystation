
using Mirror;
using UnityEngine;

/// <summary>
/// Main API for despawning objects. If you ever need to despawn something, look here
/// </summary>
public static class Despawn
{

	/// <summary>
	/// Despawn the specified game object, syncing to all clients. Despawning removes an
	/// object from the game, but may not necessarilly destroy it completely - it may end up back in the
	/// object pool to be later reused.
	/// </summary>
	/// <param name="toDespawn"></param>
	/// <returns></returns>
	public static DespawnResult ServerSingle(GameObject toDespawn)
	{
		return Server(DespawnInfo.Single(toDespawn));
	}

	/// <summary>
	/// Despawn the object server side and sync the despawn to all clients. Despawning removes an
	/// object from the game, but may not necessarilly destroy it completely - it may end up back in the
	/// object pool to be later reused.
	/// </summary>
	/// <param name="info"></param>
	/// <returns></returns>
	public static DespawnResult Server(DespawnInfo info)
	{
		if (info == null)
		{
			Logger.LogError("Cannot despawn - info is null", Category.ItemSpawn);
			return DespawnResult.Fail(info);
		}

		//even if it has a pool prefab tracker, will still destroy it if it has no object behavior
		var poolPrefabTracker = info.GameObject.GetComponent<PoolPrefabTracker>();
		var objBehavior = info.GameObject.GetComponent<ObjectBehaviour>();
		var cnt = info.GameObject.GetComponent<CustomNetTransform>();
		if (cnt)
		{
			cnt.FireGoingOffStageHooks();
		}
		else
		{
			Logger.LogWarningFormat("Attempting to network despawn object {0} at {1} but this object" +
			                        " has no CustomNetTransform. Lifecycle hooks will be bypassed. This is" +
			                        " most likely a mistake as any objects which sync over the network" +
			                        " should have a CNT.", Category.ItemSpawn, info.GameObject.name, objBehavior.AssumedWorldPositionServer());
		}
		if (poolPrefabTracker != null && objBehavior != null)
		{
			//pooled
			Spawn._AddToPool(info.GameObject);
			objBehavior.VisibleState = false;
		}
		else
		{
			//not pooled
			NetworkServer.Destroy(info.GameObject);
		}

		return DespawnResult.Single(info, info.GameObject);
	}

	/// <summary>
	/// Despawn the specified game object locally, on this client only. Despawning removes an
	/// object from the game, but may not necessarilly destroy it completely - it may end up back in the
	/// object pool to be later reused.
	/// </summary>
	/// <param name="toDespawn"></param>
	/// <returns></returns>
	public static DespawnResult ClientSingle(GameObject toDespawn)
	{
		return Client(DespawnInfo.Single(toDespawn));
	}

	/// <summary>
	/// Despawn the object locally, on this client only. Despawning removes an
	/// object from the game, but may not necessarilly destroy it completely - it may end up back in the
	/// object pool to be later reused.
	/// </summary>
	/// <param name="info"></param>
	/// <returns></returns>
	public static DespawnResult Client(DespawnInfo info)
	{
		if (info == null)
		{
			Logger.LogError("Cannot despawn - info is null", Category.ItemSpawn);
			return DespawnResult.Fail(info);
		}
		Spawn._AddToPool(info.GameObject);
		info.GameObject.SetActive(false);

		return DespawnResult.Single(info, info.GameObject);
	}
}

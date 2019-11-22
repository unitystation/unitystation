
using Mirror;
using UnityEngine;

/// <summary>
/// Spawns an indicated prefab. Can also be used as a slot populator.
/// </summary>
[CreateAssetMenu(fileName = "SpawnablePrefab", menuName = "Spawnable/SpawnablePrefab")]
public class SpawnablePrefab : Spawnable, IClientSpawnable
{
	[SerializeField]
	[Tooltip("Prefab to instantiate and populate in the slot. Must have Pickupable.")]
	private GameObject Prefab;

	public override SpawnableResult SpawnIt(SpawnDestination destination)
	{
		return new Spawnable(Prefab).SpawnAt(destination);
	}


	public SpawnableResult ClientSpawnAt(SpawnDestination destination)
	{
		return new Spawnable(Prefab).ClientSpawnAt(destination);
	}

	/// <summary>
	/// Gets a spawnable for spawning the indicated prefab.
	/// </summary>
	/// <param name="prefab">prefab this spawnable should spawn</param>
	/// <returns></returns>
	public static ISpawnable For(GameObject prefab)
	{
		return new Spawnable(prefab);
	}

	/// <summary>
	/// Gets a spawnable for spawning the indicated prefab.
	/// </summary>
	/// <param name="prefabName">name of prefab this spawnable should spawn</param>
	/// <returns></returns>
	public static ISpawnable For(string prefabName)
	{
		GameObject prefab = Spawn.GetPrefabByName(prefabName);
		if (prefab == null)
		{
			Logger.LogErrorFormat("Attempted to spawn prefab with name {0} which is either not an actual prefab name or" +
			                      " is a prefab which is not spawnable. Request to spawn will be ignored.", Category.ItemSpawn, prefabName);
			return null;
		}
		return new Spawnable(prefab);
	}

	/// <summary>
	/// Used internally so we don't need to create an asset at runtime when we want to spawn a prefab
	/// by name or prefab reference (rather than using a predefined SpawnablePrefab asset).
	/// Private so we don't expose this implementation detail / clutter the namespace.
	/// </summary>
	private class Spawnable : ISpawnable, IClientSpawnable
	{
		private readonly GameObject prefab;

		public Spawnable(GameObject prefab)
		{
			this.prefab = prefab;
		}

		public SpawnableResult SpawnAt(SpawnDestination destination)
		{
			if (prefab == null)
			{
				Logger.LogError("Cannot spawn, prefab to use is null", Category.ItemSpawn);
				return SpawnableResult.Fail(destination);
			}
			Logger.LogTraceFormat("Spawning using prefab {0}", Category.ItemSpawn, prefab);

			bool isPooled;

			GameObject tempObject = Spawn._PoolInstantiate(prefab, destination,
				out isPooled);

			if (!isPooled)
			{
				Logger.LogTrace("Prefab to spawn was not pooled, spawning new instance.", Category.ItemSpawn);
				NetworkServer.Spawn(tempObject);
				tempObject.GetComponent<CustomNetTransform>()
					?.NotifyPlayers(); //Sending clientState for newly spawned items
			}
			else
			{
				Logger.LogTrace("Prefab to spawn was pooled, reusing it...", Category.ItemSpawn);
			}

			return SpawnableResult.Single(tempObject, destination);
		}

		public SpawnableResult ClientSpawnAt(SpawnDestination destination)
		{
			bool isPooled; // not used for Client-only instantiation
			var go = Spawn._PoolInstantiate(prefab, destination, out isPooled);

			return SpawnableResult.Single(go, destination);
		}
	}

}

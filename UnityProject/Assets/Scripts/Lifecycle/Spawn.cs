
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Main API for all types of spawning (except players - see PlayerSpawn). If you ever need to spawn something, look here.
///
/// Notes on Lifecycle - there are various lifecycle stages components can hook into by implementing the
/// appropriate interface. see https://github.com/unitystation/unitystation/wiki/Object-Lifecycle-Best-Practices
/// </summary>
public static class Spawn
{

	//dict for looking up spawnable prefabs (prefabs that can be used to instantiate new objects in the game) by name.
	//Name is basically the only thing that
	//is similar between client / server (instance ID is not) so we're going with this approach unless naming collisions
	//somehow become a problem, to allow clients to ask the server to spawn things (if they have permission)
	private static Dictionary<string, GameObject> nameToSpawnablePrefab = new Dictionary<string, GameObject>();

	//object pool
	private static ObjectPool objectPool;

	/// <summary>
	/// Only to be used internally by lifecycle system.
	/// Current object pool holding all pooled objects.
	/// </summary>
	public static ObjectPool _ObjectPool
	{
		get
		{
			EnsureInit();
			return objectPool;
		}
	}

	//stuff needed for cloths
	//TODO: Consider refactoring so this stuff isn't needed or is injected somehow.
	//I would prefer this stuff not be public, but this is how it worked in the old system.
	private static GameObject uniCloth;
	private static GameObject uniBackpack;
	private static GameObject uniHeadSet;
	public static Dictionary<string, PlayerTextureData> RaceData = new Dictionary<string, PlayerTextureData>();

	/// <summary>
	/// Default scatter radius when spawning multiple things
	/// </summary>
	public static readonly float DefaultScatterRadius = 0.1f;

	private static void EnsureInit()
	{
		if (objectPool == null)
		{
			//Search through our resources and find each prefab that has a CNT component
			var spawnablePrefabs = Resources.FindObjectsOfTypeAll<GameObject>()
				.Where(IsPrefab)
				.OrderBy(go => go.name)
				//check if they have CNTs (thus are spawnable)
				.Where(go => go.GetComponent<CustomNetTransform>() != null);

			foreach (var spawnablePrefab in spawnablePrefabs)
			{
				if (!nameToSpawnablePrefab.ContainsKey(spawnablePrefab.name))
				{
					nameToSpawnablePrefab.Add(spawnablePrefab.name, spawnablePrefab);
				}
			}

			objectPool = new ObjectPool(PoolConfig.Instance);
		}
	}
	private static bool IsPrefab(GameObject toCheck) => !toCheck.transform.gameObject.scene.IsValid();

	/// <summary>
	/// Gets a prefab by its name
	/// </summary>
	/// <param name="prefabName">name of the prefab</param>
	/// <returns>the gameobject of the prefab</returns>
	public static GameObject GetPrefabByName(string prefabName)
	{
		EnsureInit();
		if (!nameToSpawnablePrefab.ContainsKey(prefabName))
		{
			//try to load it ourselves
			var prefab = Resources.Load<GameObject>(prefabName);
			if (prefab == null)
			{
				Logger.LogErrorFormat("Could not find prefab with name {0}, please ensure it is correctly spelled.", Category.ItemSpawn,
					prefabName);
				return null;
			}
			else
			{
				nameToSpawnablePrefab.Add(prefabName, prefab);
			}
		}
		return nameToSpawnablePrefab[prefabName];
	}

	/// <summary>
	/// All gameobjects representing prefabs that can be spawned
	/// </summary>
	public static List<GameObject> SpawnablePrefabs()
	{
		EnsureInit();
		return nameToSpawnablePrefab.Values.ToList();
	}


	/// <summary>
	/// Spawn the specified prefab, syncing it to all clients
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="localRotation">local rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerPrefab(GameObject prefab, Vector3? worldPosition = null, Transform parent = null,
		Quaternion? localRotation = null, int count = 1, float? scatterRadius = null, bool cancelIfImpassable = false)
	{
		return Server(
			SpawnInfo.Spawnable(
				SpawnablePrefab.For(prefab),
				SpawnDestination.At(worldPosition, parent, localRotation, cancelIfImpassable),
				count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab, syncing it to all clients
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="destination">destination to spawn at</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerPrefab(GameObject prefab, SpawnDestination destination, int count = 1, float? scatterRadius = null)
	{
		return Server(
			SpawnInfo.Spawnable(
				SpawnablePrefab.For(prefab),
				destination,
				count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab locally, for this client only.
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="localRotation">local rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ClientPrefab(GameObject prefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? localRotation = null, int count = 1, float? scatterRadius = null)
	{
		return Client(
			SpawnInfo.Spawnable(
				SpawnablePrefab.For(prefab),
				SpawnDestination.At(worldPosition, parent, localRotation),
				count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab, syncing it to all clients
	/// </summary>
	/// <param name="prefabName">name of prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="localRotation">local rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <param name="cancelIfImpassable">If true, the spawn will be cancelled if the location being spawned into is totally impassable.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerPrefab(string prefabName, Vector3? worldPosition = null, Transform parent = null,
		Quaternion? localRotation = null, int count = 1, float? scatterRadius = null, bool cancelIfImpassable = false)
	{
		return Server(
			SpawnInfo.Spawnable(
				SpawnablePrefab.For(prefabName),
				SpawnDestination.At(worldPosition, parent, localRotation, cancelIfImpassable),
				count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab locally, for this client only.
	/// </summary>
	/// <param name="prefabName">name of prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="localRotation">local rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ClientPrefab(string prefabName, Vector3? worldPosition = null, Transform parent = null, Quaternion? localRotation = null, int count = 1, float? scatterRadius = null)
	{
		return Client(
			SpawnInfo.Spawnable(
				SpawnablePrefab.For(prefabName),
				SpawnDestination.At(worldPosition, parent, localRotation),
				count, scatterRadius));
	}

	/// <summary>
	/// Clone the item and syncs it over the network. This only works if toClone has a PoolPrefabTracker
	/// attached or its name matches a prefab name, otherwise we don't know what prefab to create.
	/// </summary>
	/// <param name="toClone">GameObject to clone. This only works if toClone has a PoolPrefabTracker
	/// attached or its name matches a prefab name, otherwise we don't know what prefab to create.. Intended to work for any object, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which might need unique behavior for how they should spawn. If you are trying
	/// to clone something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented IOnStageServer or IOnStageClient when IsCloned = true</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="localRotation">local rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerClone(GameObject toClone, Vector3? worldPosition = null, Transform parent = null,
		Quaternion? localRotation = null)
	{
		return Server(
			SpawnInfo.Clone(toClone, SpawnDestination.At(worldPosition, parent, localRotation)));
	}

	/// <summary>
	/// Server-side only. Performs the spawn and syncs it to all clients.
	/// </summary>
	/// <returns></returns>
	private static SpawnResult Server(SpawnInfo info)
	{
		if (info == null)
		{
			Logger.LogError("Cannot spawn, info is null", Category.ItemSpawn);
			return SpawnResult.Fail(info);
		}

		EnsureInit();
		Logger.LogTraceFormat("Server spawning {0}", Category.ItemSpawn, info);

		List<GameObject> spawnedObjects = new List<GameObject>();
		for (int i = 0; i < info.Count; i++)
		{
			var result = info.SpawnableToSpawn.SpawnAt(info.SpawnDestination);

			if (result.Successful)
			{
				spawnedObjects.Add(result.GameObject);
				//apply scattering if it was specified
				if (info.ScatterRadius != null)
				{
					foreach (var spawned in spawnedObjects)
					{
						var cnt = spawned.GetComponent<CustomNetTransform>();
						var scatterRadius = info.ScatterRadius.GetValueOrDefault(0);
						if (cnt != null)
						{
							cnt.SetPosition(info.SpawnDestination.WorldPosition + new Vector3(Random.Range(-scatterRadius, scatterRadius), Random.Range(-scatterRadius, scatterRadius)));
						}
					}
				}
			}
			else
			{
				return SpawnResult.Fail(info);
			}
		}

		//fire hooks for all spawned objects
		SpawnResult spawnResult = null;
		if (spawnedObjects.Count == 1)
		{
			spawnResult = SpawnResult.Single(info, spawnedObjects[0]);

		}
		else
		{
			spawnResult = SpawnResult.Multiple(info, spawnedObjects);
		}

		_ServerFireClientServerSpawnHooks(spawnResult);

		return spawnResult;

	}

	/// <summary>
	/// Performs the specified spawn locally, for this client only. Must not be called on
	/// networked objects (objects with NetworkIdentity), or you will get unpredictable behavior.
	/// </summary>
	/// <returns></returns>
	public static SpawnResult Client(SpawnInfo info)
	{
		if (info == null)
		{
			Logger.LogError("Cannot spawn, info is null", Category.ItemSpawn);
			return SpawnResult.Fail(info);
		}

		if (info.SpawnableToSpawn is IClientSpawnable clientSpawnable)
		{
			List<GameObject> spawnedObjects = new List<GameObject>();
			for (var i = 0; i < info.Count; i++)
			{
				var result = clientSpawnable.ClientSpawnAt(info.SpawnDestination);
				if (result.Successful)
				{
					spawnedObjects.Add(result.GameObject);
				}
			}

			if (spawnedObjects.Count == 1)
			{
				return SpawnResult.Single(info, spawnedObjects[0]);
			}

			return SpawnResult.Multiple(info, spawnedObjects);
		}
		else
		{
			Logger.LogErrorFormat("Cannot spawn {0} client side, spawnable does not" +
			                      " implement IClientSpawnable", Category.ItemSpawn, info);
			return SpawnResult.Fail(info);
		}
	}

	/// <summary>
	/// NOTE: For internal lifecycle system use only.
	///
	/// Fires all the server side spawn hooks for the given spawn and messages all clients telling them to fire their
	/// client-side hooks. Should only be called after object becomes networked / known by clients.
	/// </summary>
	/// <param name="result"></param>
	public static void _ServerFireClientServerSpawnHooks(SpawnResult result)
	{
		//fire server hooks
		foreach (var spawnedObject in result.GameObjects)
		{
			var comps = spawnedObject.GetComponents<IServerSpawn>();
			if (comps != null)
			{
				foreach (var comp in comps)
				{
					comp.OnSpawnServer(result.SpawnInfo);
				}
			}
		}
	}

	/// <summary>
	/// Tries to determine the prefab that was used to create the specified object.
	/// If there is an attached PoolPrefabTracker, uses that. Otherwise, uses the name
	/// and removes parentheses  like (Clone) or (1) to look up the prefab name in our map.
	/// </summary>
	/// <param name="instance">object whose prefab should be determined.</param>
	/// <returns>the prefab, otherwise null if it could not be determined.</returns>
	public static GameObject DeterminePrefab(GameObject instance)
	{
		var tracker = instance.GetComponent<PoolPrefabTracker>();
		if (tracker != null)
		{
			return tracker.myPrefab;
		}

		//regex below strips out parentheses and things between them
		var prefabName = Regex.Replace(instance.name, @"\(.*\)", "").Trim();

		return nameToSpawnablePrefab.ContainsKey(prefabName) ? GetPrefabByName(prefabName) : null;
	}

	/// <summary>
	/// Clears out the object pools if necessary.
	/// </summary>
	public static void _ClearPools()
	{
		//we do this rather than using _ObjectPool to avoid EnsureInit() because
		//otherwise at the start of the first round it would create, clear, and immediately
		//re-create the pool.
		objectPool?.Clear();
	}
}

/// <summary>
/// ADded dynamically to objects when they are spawned from a prefab, to track which prefab
/// they were spawned from. Should NEVER be manually added to a prefab via inspector.
/// Only added to objects which can be pooled.
/// </summary>
public class PoolPrefabTracker : MonoBehaviour
{
	public GameObject myPrefab;
}

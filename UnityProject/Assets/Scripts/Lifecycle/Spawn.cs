
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Facepunch.Steamworks;
using Mirror;
using UnityEngine;

/// <summary>
/// Main API for all types of spawning. If you ever need to spawn something, look here.
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
	private static Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();

	//stuff needed for cloths
	//TODO: Consider refactoring so this stuff isn't needed or is injected somehow.
	//I would prefer this stuff not be public, but this is how it worked in the old system.
	private static GameObject uniCloth;
	private static GameObject uniBackpack;
	private static GameObject uniHeadSet;
	public static Dictionary<string, PlayerTextureData> RaceData = new Dictionary<string, PlayerTextureData>();
	public static Dictionary<string, ClothingData> ClothingStoredData = new Dictionary<string, ClothingData>();
	public static Dictionary<string, ContainerData> BackpackStoredData = new Dictionary<string, ContainerData>();
	public static Dictionary<string, HeadsetData> HeadSetStoredData = new Dictionary<string, HeadsetData>();
	public static Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>> PlayerCustomisationData =
		new Dictionary<PlayerCustomisation, Dictionary<string, PlayerCustomisationData>>();

	/// <summary>
	/// Default scatter radius when spawning multiple things
	/// </summary>
	public static readonly float DefaultScatterRadius = 0.1f;

	private static void EnsureInit()
	{
		if (nameToSpawnablePrefab.Count == 0)
		{
			//Search through our resources and find each prefab that has a CNT component
			var spawnablePrefabs = Resources.FindObjectsOfTypeAll<GameObject>()
				.Where(IsPrefab)
				.OrderBy(go => go.name)
				//check if they have CNTs (thus are spawnable)
				.Where(go => go.GetComponent<CustomNetTransform>() != null);

			foreach (var spawnablePrefab in spawnablePrefabs)
			{
				nameToSpawnablePrefab.Add(spawnablePrefab.name, spawnablePrefab);
			}
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
	/// Spawns the cloth with the specifeid cloth data on the server, syncing it to clients
	/// </summary>
	/// <param name="clothData">cloth data describing the cloth, should be a subtype of BaseClothData</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="CVT">variant type to spawn this cloth as, defaults to Default</param>
	/// <param name="variantIndex">variant index to spawn this cloth as, defaults to -1</param>
	/// <param name="prefabOverride">prefab to use instead of this cloth's default</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns></returns>
	public static SpawnResult ServerCloth(BaseClothData clothData, Vector3? worldPosition = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variantIndex = -1, GameObject prefabOverride = null,
		Transform parent = null, Quaternion? rotation = null,
		int count = 1, float? scatterRadius = null)
	{
		return Server(
			SpawnInfo.Cloth(clothData, worldPosition, CVT, variantIndex, prefabOverride, parent, rotation, count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab, syncing it to all clients
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerPrefab(GameObject prefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null, int count = 1, float? scatterRadius = null)
	{
		return Server(SpawnInfo.Prefab(prefab, worldPosition, parent, rotation, count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab locally, for this client only.
	/// </summary>
	/// <param name="prefab">Prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ClientPrefab(GameObject prefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null, int count = 1, float? scatterRadius = null)
	{
		return Client(SpawnInfo.Prefab(prefab, worldPosition, parent, rotation, count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab, syncing it to all clients
	/// </summary>
	/// <param name="prefabName">name of prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerPrefab(string prefabName, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null, int count = 1, float? scatterRadius = null)
	{
		return Server(SpawnInfo.Prefab(prefabName, worldPosition, parent, rotation, count, scatterRadius));
	}

	/// <summary>
	/// Spawn the specified prefab locally, for this client only.
	/// </summary>
	/// <param name="prefabName">name of prefab to spawn an instance of. This is intended to be made to work for pretty much any prefab, but don't
	/// be surprised if it doesn't as there are LOTS of prefabs in the game which all have unique behavior for how they should spawn. If you are trying
	/// to instantiate something and it isn't properly setting itself up, check to make sure each component that needs to set something up has
	/// properly implemented necessary lifecycle methods.</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ClientPrefab(string prefabName, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null, int count = 1, float? scatterRadius = null)
	{
		return Client(SpawnInfo.Prefab(prefabName, worldPosition, parent, rotation, count, scatterRadius));
	}

	/// <summary>
	/// Spawn a player with the specified occupation, syncing it to all clients
	/// </summary>
	/// <param name="occupation">Occupation details to use to spawn this player</param>
	/// <param name="characterSettings">settings to use for this player</param>
	/// <param name="playerPrefab">Prefab to use to spawn this player</param>
	/// <param name="worldPosition">world position to appear at. Defaults to HiddenPos (hidden / invisible)</param>
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <param name="count">number of instances to spawn, defaults to 1</param>
	/// <param name="scatterRadius">radius to scatter the spawned instances by from their spawn position. Defaults to
	/// null (no scatter).</param>
	/// <returns>the newly created GameObject</returns>
	/// <returns></returns>
	public static SpawnResult ServerPlayer(Occupation occupation, CharacterSettings characterSettings, GameObject playerPrefab, Vector3? worldPosition = null, Transform parent = null, Quaternion? rotation = null)
	{
		return Server(SpawnInfo.Player(occupation, characterSettings, playerPrefab, worldPosition, parent, rotation));
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
	/// <param name="rotation">rotation to spawn with, defaults to Quaternion.identity</param>
	/// <param name="parent">Parent to spawn under, defaults to no parent. Most things
	/// should always be spawned under the Objects transform in their matrix. Many objects (due to RegisterTile)
	/// usually take care of properly parenting themselves when spawned so in many cases you can leave it null.</param>
	/// <returns>the newly created GameObject</returns>
	public static SpawnResult ServerClone(GameObject toClone, Vector3? worldPosition = null, Transform parent = null,
		Quaternion? rotation = null)
	{
		return Server(SpawnInfo.Clone(toClone, worldPosition, parent, rotation));
	}

	/// <summary>
	/// Server-side only. Performs the spawn and syncs it to all clients.
	/// </summary>
	/// <returns></returns>
	public static SpawnResult Server(SpawnInfo info)
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
			if (info.SpawnableType == SpawnableType.Prefab)
			{
				bool isPooled;

				GameObject tempObject = PoolInstantiate(info.PrefabUsed, info.WorldPosition, info.Rotation, info.Parent,
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

				spawnedObjects.Add(tempObject);
			}
			else if (info.SpawnableType == SpawnableType.Cloth)
			{
				var result = ServerCloth(info);
				if (result == null)
				{
					return SpawnResult.Fail(info);
				}

				spawnedObjects.Add(result);
			}
			else if (info.SpawnableType == SpawnableType.Clone)
			{
				var prefab = DeterminePrefab(info.ClonedFrom);
				if (prefab == null)
				{
					Logger.LogErrorFormat("Object {0} cannot be cloned because it has no PoolPrefabTracker and its name" +
					                      " does not match a prefab name, so we cannot" +
					                      " determine the prefab to instantiate. Please fix this object so that it" +
					                      " has an attached PoolPrefabTracker or so its name matches the prefab it was created from.", Category.ItemSpawn, info.ClonedFrom);
				}
				GameObject tempObject = PoolInstantiate(prefab, info.WorldPosition, info.Rotation, info.Parent, out var isPooled);

				if (!isPooled)
				{
					NetworkServer.Spawn(tempObject);
					tempObject.GetComponent<CustomNetTransform>()?.NotifyPlayers();//Sending clientState for newly spawned items
				}

				spawnedObjects.Add(tempObject);
			}
			//fire hooks for all spawned objects
			if (spawnedObjects.Count == 1)
			{
				ServerFireSpawnHooks(SpawnResult.Single(info, spawnedObjects[0]));
			}
			else
			{
				ServerFireSpawnHooks(SpawnResult.Multiple(info, spawnedObjects));
			}
		}

		return SpawnResult.Multiple(info, spawnedObjects);
	}

	/// <summary>
	/// Performs the specified spawn locally, for this client only.
	/// </summary>
	/// <returns></returns>
	public static SpawnResult Client(SpawnInfo info)
	{
		if (info == null)
		{
			Logger.LogError("Cannot spawn, info is null", Category.ItemSpawn);
			return SpawnResult.Fail(info);
		}
		List<GameObject> spawnedObjects = new List<GameObject>();
		for (var i = 0; i < info.Count; i++)
		{
			if (info.SpawnableType == SpawnableType.Cloth)
			{
				Logger.LogErrorFormat("Spawning cloths on client side is not currently supported. {0}", Category.ItemSpawn, info);
				return SpawnResult.Fail(info);
			}

			bool isPooled; // not used for Client-only instantiation
			var go = PoolInstantiate(info.PrefabUsed, info.WorldPosition, info.Rotation, info.Parent, out isPooled);

			spawnedObjects.Add(go);
		}

		//fire client side lifecycle hooks
		foreach (var spawnedObject in spawnedObjects)
		{
			var hooks = spawnedObject.GetComponents<IClientSpawn>();
			if (hooks != null)
			{
				foreach (var hook in hooks)
				{
					hook.OnSpawnClient(ClientSpawnInfo.Default());
				}
			}
		}

		if (spawnedObjects.Count == 1)
		{
			return SpawnResult.Single(info, spawnedObjects[0]);
		}

		return SpawnResult.Multiple(info, spawnedObjects);
	}

	private static GameObject ServerCloth(SpawnInfo info)
	{
		return CreateCloth(info.ClothData, info.WorldPosition, info.Parent, info.ClothingVariantType,
			info.ClothingVariantIndex, info.PrefabUsed);
	}

	/// <summary>
	/// Spawns the indicated cloth.
	/// </summary>
	/// <param name="ClothingData">data describing the cloth to spawn</param>
	/// <param name="worldPos"></param>
	/// <param name="parent"></param>
	/// <param name="CVT"></param>
	/// <param name="variant"></param>
	/// <param name="PrefabOverride">prefab to use instead of the default for this cloth type</param>
	/// <returns></returns>
	private static GameObject CreateCloth(BaseClothData clothData, Vector3? worldPos = null, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (clothData is HeadsetData headsetData)
		{
			return CreateHeadsetCloth(headsetData, worldPos, parent, CVT, variant, PrefabOverride);
		}
		else if (clothData is ContainerData containerData)
		{
			return CreateBackpackCloth(containerData, worldPos, parent, CVT, variant, PrefabOverride);
		}
		else if (clothData is ClothingData clothingData)
		{
			return CreateCloth(clothingData, worldPos, parent, CVT, variant, PrefabOverride);
		}
		else
		{
			Logger.LogErrorFormat("Unrecognize BaseClothData subtype {0}, please add logic" +
			                      " to ClothFactory to handle spawning this type.", Category.ItemSpawn,
				clothData.GetType().Name);
			return null;
		}
	}

	private static GameObject CreateHeadsetCloth(HeadsetData headsetData, Vector3? worldPos = null, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (uniHeadSet == null)
		{
			uniHeadSet = GetPrefabByName("UniHeadSet");
		}
		if (uniHeadSet == null)
		{
			Logger.LogError("UniHeadSet Prefab not found", Category.SpriteHandler);
			return null;
		}

		GameObject clothObj;

		if (PrefabOverride != null && PrefabOverride != uniHeadSet)
		{
			clothObj = Spawn.ServerPrefab(PrefabOverride, worldPos, parent).GameObject;
		}
		else
		{
			clothObj = Spawn.ServerPrefab(uniHeadSet, worldPos, parent).GameObject;
		}

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		var Headset = clothObj.GetComponent<Headset>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(headsetData.Sprites.Equipped);
		_Clothing.SetSynchronise(HD: headsetData);
		Item.SetUpFromClothingData(headsetData.Sprites, headsetData.ItemAttributes);
		Headset.EncryptionKey = headsetData.Key.EncryptionKey;
		return clothObj;
	}

	/// <summary>
	/// Fires all the server side spawn hooks and messages the client telling them to fire their
	/// client-side hooks.
	/// </summary>
	/// <param name="result"></param>
	private static void ServerFireSpawnHooks(SpawnResult result)
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

		//fire client hooks
		SpawnMessage.SendToAll(result);
	}


	private static GameObject CreateBackpackCloth(ContainerData ContainerData, Vector3? worldPos = null, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (uniBackpack == null)
		{
			uniBackpack = GetPrefabByName("UniBackPack");
		}
		if (uniBackpack == null)
		{
			Logger.LogError("UniBackPack Prefab not found", Category.SpriteHandler);
			return null;
		}

		GameObject clothObj;
		if (PrefabOverride != null && PrefabOverride != uniBackpack)
		{
			clothObj = Spawn.ServerPrefab(PrefabOverride, worldPos, parent).GameObject;
		}
		else
		{
			clothObj = Spawn.ServerPrefab(uniBackpack, worldPos, parent).GameObject;
		}

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetupSingleSprite(ContainerData.Sprites.Equipped);
		Item.SetUpFromClothingData(ContainerData.Sprites, ContainerData.ItemAttributes);
		_Clothing.SetSynchronise(ConD: ContainerData);
		return clothObj;
	}


	private static GameObject CreateCloth(ClothingData ClothingData, Vector3? worldPos = null, Transform parent = null,
		ClothingVariantType CVT = ClothingVariantType.Default, int variant = -1, GameObject PrefabOverride = null)
	{
		if (uniCloth == null)
		{
			uniCloth = GetPrefabByName("UniCloth");
		}
		if (uniCloth == null)
		{
			Logger.LogError("UniCloth Prefab not found", Category.SpriteHandler);
			return null;
		}

		GameObject clothObj;
		if (PrefabOverride != null && PrefabOverride != uniCloth)
		{
			clothObj = Spawn.ServerPrefab(PrefabOverride, worldPos, parent).GameObject;
		}
		else
		{
			clothObj = Spawn.ServerPrefab(uniCloth, worldPos, parent).GameObject;
		}

		var _Clothing = clothObj.GetComponent<Clothing>();
		var Item = clothObj.GetComponent<ItemAttributes>();
		_Clothing.SpriteInfo = StaticSpriteHandler.SetUpSheetForClothingData(ClothingData, _Clothing);
		_Clothing.SetSynchronise(CD: ClothingData);
		Item.SetUpFromClothingData(ClothingData.Base, ClothingData.ItemAttributes);
		switch (CVT)
		{
			case ClothingVariantType.Default:
				if (variant > -1)
				{
					if (!(ClothingData.Variants.Count >= variant))
					{
						Item.SetUpFromClothingData(ClothingData.Variants[variant], ClothingData.ItemAttributes);
					}
				}

				break;
			case ClothingVariantType.Skirt:
				Item.SetUpFromClothingData(ClothingData.DressVariant, ClothingData.ItemAttributes);
				break;
			case ClothingVariantType.Tucked:
				Item.SetUpFromClothingData(ClothingData.Base_Adjusted, ClothingData.ItemAttributes);
				break;
		}

		clothObj.name = ClothingData.name;
		return clothObj;
	}

	private static GameObject PoolInstantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, out bool pooledInstance)
	{
		GameObject tempObject = null;
		bool hide = position == TransformState.HiddenPos;
		//Cut off Z-axis
		Vector3 cleanPos = ( Vector2 ) position;
		Vector3 pos = hide ? TransformState.HiddenPos : cleanPos;
		if (CanLoadFromPool(prefab))
		{
			//pool exists and has unused instances
			int index = pools[prefab].Count - 1;
			tempObject = pools[prefab][index];
			pools[prefab].RemoveAt(index);
			tempObject.SetActive(true);

			ObjectBehaviour objBehaviour = tempObject.GetComponent<ObjectBehaviour>();
			if (objBehaviour)
			{
				objBehaviour.VisibleState = !hide;
			}
			tempObject.transform.position = pos;
			tempObject.transform.rotation = rotation;
			tempObject.transform.localScale = prefab.transform.localScale;
			tempObject.transform.parent = parent;
			var cnt = tempObject.GetComponent<CustomNetTransform>();
			if ( cnt )
			{
				cnt.ReInitServerState();
				cnt.NotifyPlayers(); //Sending out clientState for already spawned items
			}

			pooledInstance = true;
		}
		else
		{
			tempObject = Object.Instantiate(prefab, pos, rotation, parent);

			tempObject.GetComponent<CustomNetTransform>()?.ReInitServerState();

			tempObject.AddComponent<PoolPrefabTracker>().myPrefab = prefab;

			pooledInstance = false;
		}

		return tempObject;

	}

	private static bool CanLoadFromPool(GameObject prefab)
	{
		return pools.ContainsKey(prefab) && pools[prefab].Count > 0;
	}

	/// <summary>
	/// For internal use (lifecycle system) only
	/// </summary>
	/// <param name="target"></param>
	public static void _AddToPool(GameObject target)
	{
		var poolPrefabTracker = target.GetComponent<PoolPrefabTracker>();
		if ( !poolPrefabTracker )
		{
			Logger.LogWarning($"PoolPrefabTracker not found on {target}",Category.ItemSpawn);
			return;
		}
		GameObject prefab = poolPrefabTracker.myPrefab;
		prefab.transform.position = Vector2.zero;

		if (!pools.ContainsKey(prefab))
		{
			//pool for this prefab does not yet exist
			pools.Add(prefab, new List<GameObject>());
		}

		pools[prefab].Add(target);
	}


	/// <summary>
	/// FOR DEV / TESTING ONLY! Simulates destroying and recreating an item by putting it in the pool and taking it back
	/// out again. If item is not pooled, simply destroys and recreates it as if calling Despawn and then Spawn
	/// Can use this to validate that the object correctly re-initializes itself after spawning -
	/// no state should be left over from its previous incarnation.
	/// </summary>
	/// <returns>the re-created object</returns>
	public static GameObject ServerPoolTestRespawn(GameObject target)
	{
		var objBehavior = target.GetComponent<ObjectBehaviour>();
		if (objBehavior == null)
		{
			//destroy / create using normal approach with no pooling
			Logger.LogWarningFormat("Object {0} has no object behavior, thus cannot be pooled. It will be destroyed / created" +
			                        " without going through the pool.", Category.ItemSpawn, target.name);

			//determine prefab
			var position = target.TileWorldPosition();
			var prefab = DeterminePrefab(target);
			if (prefab == null)
			{
				Logger.LogErrorFormat("Object {0} at {1} cannot be respawned because it has no PoolPrefabTracker and its name" +
				                      " does not match a prefab name, so we cannot" +
				                      " determine the prefab to instantiate. Please fix this object so that it" +
				                      " has an attached PoolPrefabTracker or so its name matches the prefab it was created from.", Category.ItemSpawn, target.name, position);
				return null;
			}

			Despawn.ServerSingle(target);
			return ServerPrefab(prefab, position.To3Int()).GameObject;
		}
		else
		{
			//destroy / create with pooling
			//save previous position
			var worldPos = objBehavior.AssumedWorldPositionServer();

			//this simulates going into the pool
			Despawn._ServerFireDespawnHooks(DespawnResult.Single(DespawnInfo.Single(target)));

			objBehavior.VisibleState = false;

			//this simulates coming back out of the pool
			target.SetActive(true);
			objBehavior.VisibleState = true;

			target.transform.position = worldPos;
			var cnt = target.GetComponent<CustomNetTransform>();
			if (cnt)
			{
				cnt.ReInitServerState();
				cnt.NotifyPlayers(); //Sending out clientState for already spawned items
			}

			SpawnInfo spawnInfo = null;
			//cloth or prefab?
			var clothing = target.GetComponent<Clothing>();
			var prefab = DeterminePrefab(target);
			if (clothing != null)
			{
				spawnInfo = SpawnInfo.Cloth(clothing.clothingData, worldPos, clothing.Type,
					clothing.Variant, prefab);
			}
			else
			{
				spawnInfo = SpawnInfo.Prefab(prefab, worldPos);
			}

			ServerFireSpawnHooks(SpawnResult.Single(spawnInfo, target));
			return target;
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
}

//not used for clients unless it is a client side pool object only
public class PoolPrefabTracker : MonoBehaviour
{
	public GameObject myPrefab;
}

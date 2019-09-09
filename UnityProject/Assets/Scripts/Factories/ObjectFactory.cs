
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Util class for spawning things. Also allows spawning stuff without needing to save their
/// prefab references in every component instance.
/// </summary>
public static class ObjectFactory
{
	private static GameObject metalPrefab;
	private static GameObject glassShardPrefab;
	private static GameObject rodsPrefab;
	private static GameObject plasmaPrefab;

	private static bool hasInit = false;

	private static void EnsureInit()
	{
		if (hasInit) return;
		metalPrefab = Resources.Load<GameObject>("Metal");
		glassShardPrefab = Resources.Load("GlassShard") as GameObject;
		rodsPrefab = Resources.Load("Rods") as GameObject;
		plasmaPrefab = Resources.Load("SolidPlasma") as GameObject;
		hasInit = true;
	}

	private static void Spawn(int amount, GameObject prefab, Vector2Int tileWorldPosition, float scatterRadius, Transform parent, Action<GameObject> andThen=null)
	{
		for (int i = 0; i < amount; i++)
		{
			var spawned = PoolManager.PoolNetworkInstantiate(prefab, tileWorldPosition.To3Int(), parent);
			if (scatterRadius > 0)
			{
				var cnt = spawned.GetComponent<CustomNetTransform>();
				if (cnt != null)
				{
					cnt.SetPosition(cnt.ServerState.WorldPosition + new Vector3(Random.Range(-scatterRadius, scatterRadius), Random.Range(-scatterRadius, scatterRadius)));
				}
			}

			if (andThen != null)
			{
				andThen(spawned);
			}
		}
	}

	public static void SpawnMetal(int amount, Vector2Int tileWorldPosition, float scatterRadius = 0.1f, Transform parent=null)
	{
		EnsureInit();
		Spawn(amount, metalPrefab, tileWorldPosition, scatterRadius, parent);
	}

	public static void SpawnGlassShard(int amount, Vector2Int tileWorldPosition, float scatterRadius = 0.4f, Transform parent=null)
	{
		EnsureInit();
		Spawn(amount, glassShardPrefab, tileWorldPosition, scatterRadius, parent, go => go.GetComponent<GlassShard>().SetRandomSprite());
	}

	public static void SpawnRods(int amount, Vector2Int tileWorldPosition, float scatterRadius = 0.1f, Transform parent=null)
	{
		EnsureInit();
		Spawn(amount, rodsPrefab, tileWorldPosition, scatterRadius, parent);
	}

	public static void SpawnPlasma(int amount, Vector2Int tileWorldPosition, float scatterRadius = 0.1f, Transform parent=null)
	{
		EnsureInit();
		Spawn(amount, plasmaPrefab, tileWorldPosition, scatterRadius, parent);
	}

}

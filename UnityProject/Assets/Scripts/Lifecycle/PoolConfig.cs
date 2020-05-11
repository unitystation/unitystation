using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Configuration for the object pool, which re-uses frequently-spawned objects to avoid expensive Instantiate calls.
///
/// A pool consists of multiple prefab pools, each with a specific capacity for instances of each prefab.
/// </summary>
[CreateAssetMenu(fileName = "PoolConfig", menuName = "Singleton/PoolConfig")]
public class PoolConfig : SingletonScriptableObject<PoolConfig>
{

	[System.Serializable]
	private class PrefabPoolConfig
	{
		[Tooltip("Prefab this pool will hold instances of. Does not" +
		         " apply to variants of this prefab. Each variant that should" +
		         " be pooled needs its own entry.")]
		public GameObject Prefab;

		[Tooltip("Amount of instances of the indicated prefab this pool can hold.")]
		public int Capacity;
	}

	[Tooltip("Configuration for each prefab that can be pooled. Adding" +
	         " a prefab to this list will make it pool-able.")]
	[SerializeField]
	[ArrayElementTitle("Prefab")]
	private PrefabPoolConfig[] prefabPools;

	// cached for fast lookup of prefab pool configs
	private Dictionary<GameObject, PrefabPoolConfig> prefabToConfig;

	private void Awake()
	{
		OnValidate();
	}

	public void OnValidate()
	{
		prefabToConfig = new Dictionary<GameObject, PrefabPoolConfig>();
		foreach (var prefabConfig in prefabPools)
		{
			if (prefabConfig.Prefab != null && !prefabToConfig.ContainsKey(prefabConfig.Prefab))
			{
				prefabToConfig.Add(prefabConfig.Prefab, prefabConfig);
			}
		}
	}

	/// <summary>
	/// Checks if instances of the indicated prefab can be pooled (regardless of
	/// current pool capacity)
	/// </summary>
	/// <param name="prefab">prefab to check</param>
	/// <returns>true iff instances of this prefab are allowed to be pooled</returns>
	public bool IsPoolable(GameObject prefab)
	{
		return prefabToConfig.ContainsKey(prefab);
	}

	/// <summary>
	/// Checks the configured max capacity of the pool for the indicated prefab.
	/// </summary>
	/// <param name="prefab">prefab to check</param>
	/// <returns></returns>
	public int GetCapacity(GameObject prefab)
	{
		if (prefabToConfig.TryGetValue(prefab, out var prefabPoolConfig))
		{
			return prefabPoolConfig.Capacity;
		}

		return 0;
	}
}

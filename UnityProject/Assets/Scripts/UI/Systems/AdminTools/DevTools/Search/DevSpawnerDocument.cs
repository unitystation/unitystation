using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Util;

namespace UI.Systems.AdminTools.DevTools.Search
{
	/// <summary>
	/// A document in our dev spawner search (lives in our Trie nodes), representing something spawnable. Currently only supports prefabs
	/// but could be extended to support other capabilities.
	/// </summary>
	public struct DevSpawnerDocument
	{
		/// <summary>
		/// Prefab this document represents.
		/// </summary>
		public readonly GameObject Prefab;
		/// <summary>
		/// Name cleaned up for searchability (like lowercase).
		/// </summary>
		public readonly List<string> SearchableName;

		public bool IsDEBUG;

		private DevSpawnerDocument(GameObject prefab, bool _isDebug)
		{
			IsDEBUG = _isDebug;
			Prefab = prefab;
			SearchableName = new List<string>();
			SearchableName.Add(SpawnerSearch.Standardize(prefab.name));
			if (prefab.TryGetComponent<PrefabTracker>(out var tracker) == false) return;
			if (tracker.CanBeSpawnedByAdmin == false) return;
			if (string.IsNullOrWhiteSpace(tracker.AlternativePrefabName) == false) SearchableName.Add(tracker.AlternativePrefabName);
		}

		/// <summary>
		/// Create a dev spawner document representing this prefab.
		/// </summary>
		/// <param name="prefab"></param>
		public static DevSpawnerDocument? ForPrefab(GameObject prefab)
		{
			bool isDebug = false;

			if (prefab.TryGetComponent<PrefabTracker>(out var tracker) && tracker.CanBeSpawnedByAdmin == false) isDebug = true;

			return new DevSpawnerDocument(prefab, isDebug);
		}
	}
}


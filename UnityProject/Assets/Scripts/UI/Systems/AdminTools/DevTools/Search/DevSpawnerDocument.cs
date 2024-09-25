using System;
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
		public readonly string[] SearchableName;

		public readonly string[] RelatedPrefabsIDs;

		public readonly string Name;

		public readonly string ForeverID;

		public bool IsDEBUG;

		private DevSpawnerDocument(GameObject prefab, bool _isDebug)
		{
			//TODO (Bod (Max made me say this because she has an addiction to refactoring everything in this project))
			//TODO : this will get reworked by Max at some point because she wanted to update this menu to also be workable in creative mode.
			IsDEBUG = _isDebug;
			Prefab = prefab;
			var SearchableNameList = new List<string>();
			var RelatedPrefabsIDsList = new List<string>();
			Name = prefab.name;
			SearchableNameList.Add(SpawnerSearch.Standardize(prefab.name));
			if (prefab.TryGetComponent<PrefabTracker>(out var tracker) == false)
			{
				SearchableName = SearchableNameList.ToArray();
				ForeverID = "";
				RelatedPrefabsIDs = Array.Empty<string>();
				return;
			}
			RelatedPrefabsIDsList.Add(tracker.ForeverID);
			ForeverID = tracker.ForeverID;
			if (string.IsNullOrWhiteSpace(tracker.AlternativePrefabName) == false) SearchableNameList.Add(tracker.AlternativePrefabName);

			while (tracker != null)
			{
				if (RelatedPrefabsIDsList.Contains(tracker.ParentID) ==false)
				{
					RelatedPrefabsIDsList.Add(tracker.ParentID);
				}

				if (CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs.ContainsKey(tracker.ParentID))
				{
					tracker = CustomNetworkManager.Instance.ForeverIDLookupSpawnablePrefabs[tracker.ParentID].OrNull()?.GetComponent<PrefabTracker>();
				}
				else
				{
					tracker = null;
				}

			}
			SearchableName = SearchableNameList.ToArray();
			RelatedPrefabsIDs = RelatedPrefabsIDsList.ToArray();
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


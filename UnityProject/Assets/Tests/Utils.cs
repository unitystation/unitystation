using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Tests
{
	public static class Utils
	{
		/// <summary>
		/// Cached assets array to pass to FindAssets
		/// </summary>
		private static readonly string[] AssetsArr = { "Assets" };

		/// <summary>
		/// Returns a sequence of any scenes not found in the ActiveScenes and DevScenes folders.
		/// </summary>
		public static IEnumerable<string> NonDevScenes
		{
			get
			{
				return GUIDsToPaths(FindGUIDsOfType("Scene", "Scenes"),
					s => (s.Contains("ActiveScenes")
						|| s.Contains("DevScenes")
						|| s.StartsWith("Packages")) == false);
			}
		}

		/// <summary>
		/// Finds all prefabs located in the prefabs folder and returns them as GameObjects.
		/// </summary>
		/// <param name="onlyPrefabsFolder">Should it retrieve only prefabs in the main Prefabs folder</param>
		/// <param name="pathFilter">A predicate to filter found prefab paths.</param>
		public static IEnumerable<GameObject> FindPrefabs(
			bool onlyPrefabsFolder = true,
			Predicate<string> pathFilter = null)
		{
			return GUIDsToAssets<GameObject>(
				FindGUIDsOfType("prefab", onlyPrefabsFolder ? "Prefabs" : null), pathFilter);
		}

		/// <summary>
		/// Finds all assets of a specific type and returns the loaded assets.
		/// </summary>
		/// <param name="inFolder">Run the search in a specific folder. Auto prefixes with Assets Folder.</param>
		/// <param name="pathFilter">A predicate to filter found asset paths.</param>
		public static IEnumerable<T> FindAssetsByType<T>(string inFolder = null, Predicate<string> pathFilter = null)
			where T : Object
		{
			return GUIDsToAssets<T>(FindGUIDsOfType(typeof(T).Name, inFolder), pathFilter);
		}

		/// <summary>
		/// Finds all assets of a specific type and returns the asset guids.
		/// </summary>
		/// <param name="type">The type of asset to find.</param>
		/// <param name="inFolder">Search in a specific folder. Auto prefixes with Assets folder.</param>
		public static string[] FindGUIDsOfType(string type, string inFolder = null)
		{
			return AssetDatabase.FindAssets($"t:{type}",
				inFolder is null ? AssetsArr : new[] { $"Assets/{inFolder}" });
		}

		private static IEnumerable<T> GUIDsToAssets<T>(IEnumerable<string> guids, Predicate<string> pathFilter)
			where T : Object
		{
			return GUIDsToPaths(guids, pathFilter).Select(AssetDatabase.LoadAssetAtPath<T>);
		}

		/// <summary>
		/// Converts a sequence of GUIDs into path strings.
		/// </summary>
		/// <param name="guids">The GUIDs to convert</param>
		/// <param name="pathFilter">A predicate to filter found asset paths.</param>
		public static IEnumerable<string> GUIDsToPaths(IEnumerable<string> guids, Predicate<string> pathFilter)
		{
			return guids.Select(AssetDatabase.GUIDToAssetPath).Where(path => pathFilter is null || pathFilter(path));
		}

		/// <summary>
		/// Finds a single scriptable object of a given type. Throws an exception if none or more than one are found.
		/// </summary>
		public static T GetSingleScriptableObject<T>(TestReport report) where T : ScriptableObject
		{
			var typeName = typeof(T).Name;
			var guids = AssetDatabase.FindAssets($"t: {typeName}");

			report.FailIf(guids.Length, Is.EqualTo(0))
				.AppendLine($"{typeName}: could not locate {typeName}")
				.AssertPassed()
				.FailIf(guids.Length, Is.GreaterThan(1))
				.AppendLine($"{typeName}: more than one {typeName} exists!")
				.AssertPassed();

			return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids.First()));
		}

		/// <summary>
		/// Returns the object's type. Even if the object is considered Unity's null, GetType can still
		/// be accessed. If the instance is a true null, then null is returned;
		/// </summary>
		public static Type GetObjectType(Object instance) => instance is null ? null : instance.GetType();

		/// <summary>
		/// Returns the instanceID of an object. Even if the object is considered Unity's null, GetInstanceID can still
		/// be accessed. If the instance is a true null, then 0 is returned.
		/// </summary>
		public static int GetInstanceID(Object instance) => instance is null ? 0 : instance.GetInstanceID();
	}
}

using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEditor;
using UnityEngine;
using US13.ChemistryComponents;
using US13.Items.Food;

/// <summary>
/// Editor tool to find and remove duplicate Edible or ReagentContainer components on prefabs.
/// Processes prefabs in hierarchy order (parents before children) so that fixing a parent
/// automatically resolves inherited duplicates in its variants.
/// When duplicates are found, it removes the one with the oldest ancestry (deepest in the
/// variant chain) and keeps the one added closest to the current prefab level.
/// </summary>
public class DuplicateComponentCleaner : EditorWindow
{
	private Vector2 scroll;
	private List<DuplicateInfo> results = new List<DuplicateInfo>();
	private bool hasScanned;

	private struct DuplicateInfo
	{
		public string PrefabPath;
		public string ComponentType;
		public int Count;
		public int Depth;
	}

	[MenuItem("Tools/Duplicate Component Cleaner")]
	private static void Init()
	{
		var window = (DuplicateComponentCleaner)GetWindow(typeof(DuplicateComponentCleaner));
		window.titleContent.text = "Duplicate Component Cleaner";
		window.Show();
	}

	private void OnGUI()
	{
		EditorGUILayout.HelpBox(
			"Scans all prefabs for duplicate Edible or ReagentContainer components.\n" +
			"Fixes are applied parents-first so child variants inherit the fix.\n" +
			"The component with the oldest ancestry is removed, keeping the newest one.",
			MessageType.Info);

		EditorGUILayout.Space();

		if (GUILayout.Button("Scan for Duplicates"))
		{
			ScanForDuplicates();
		}

		if (hasScanned == false) return;

		EditorGUILayout.Space();

		if (results.Count == 0)
		{
			EditorGUILayout.LabelField("No duplicate components found.");
			return;
		}

		EditorGUILayout.LabelField($"Found {results.Count} prefab(s) with duplicate components:");
		EditorGUILayout.Space();

		if (GUILayout.Button($"Fix All ({results.Count} prefabs)"))
		{
			FixAllDuplicates();
		}

		EditorGUILayout.Space();

		scroll = GUILayout.BeginScrollView(scroll);
		foreach (var info in results)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label($"[depth {info.Depth}] {info.PrefabPath} — {info.ComponentType} x{info.Count}",
				GUILayout.Width(position.width - 80));
			if (GUILayout.Button("Select", GUILayout.Width(60)))
			{
				Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(info.PrefabPath);
			}
			GUILayout.EndHorizontal();
		}
		GUILayout.EndScrollView();
	}

	/// <summary>
	/// Returns how deep a prefab is in the variant chain.
	/// A root prefab returns 0, a variant of it returns 1, etc.
	/// </summary>
	private static int GetVariantDepth(string prefabPath)
	{
		int depth = 0;
		var go = AssetDatabase.LoadMainAssetAtPath(prefabPath) as GameObject;

		while (go != null)
		{
			var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
			if (source == null || source == go) break;
			depth++;
			go = source;
		}

		return depth;
	}

	/// <summary>
	/// Counts components whose exact runtime type matches T (excludes subclasses).
	/// </summary>
	private static int CountExactComponents<T>(GameObject go, List<T> buffer) where T : Component
	{
		go.GetComponents(buffer);
		int count = 0;
		foreach (var comp in buffer)
		{
			if (comp.GetType() == typeof(T)) count++;
		}
		return count;
	}

	private void ScanForDuplicates()
	{
		results.Clear();
		hasScanned = true;

		string[] allPrefabs = SearchAndDestroy.GetAllPrefabs();
		int total = allPrefabs.Length;

		var edibleBuffer = new List<Edible>();
		var containerBuffer = new List<ReagentContainer>();

		for (int i = 0; i < total; i++)
		{
			EditorUtility.DisplayProgressBar("Scanning Prefabs", $"Prefab {i + 1}/{total}", (float)i / total);

			var go = AssetDatabase.LoadMainAssetAtPath(allPrefabs[i]) as GameObject;
			if (go == null) continue;

			int depth = -1;

			int edibleCount = CountExactComponents(go, edibleBuffer);
			if (edibleCount > 1)
			{
				if (depth < 0) depth = GetVariantDepth(allPrefabs[i]);
				results.Add(new DuplicateInfo
				{
					PrefabPath = allPrefabs[i],
					ComponentType = nameof(Edible),
					Count = edibleCount,
					Depth = depth
				});
			}

			int containerCount = CountExactComponents(go, containerBuffer);
			if (containerCount > 1)
			{
				if (depth < 0) depth = GetVariantDepth(allPrefabs[i]);
				results.Add(new DuplicateInfo
				{
					PrefabPath = allPrefabs[i],
					ComponentType = nameof(ReagentContainer),
					Count = containerCount,
					Depth = depth
				});
			}
		}

		// Sort by depth so parents are listed (and fixed) before children.
		results = results.OrderBy(r => r.Depth).ThenBy(r => r.PrefabPath).ToList();

		EditorUtility.ClearProgressBar();
		Loggy.Info().Format("Duplicate scan complete. Found {0} prefab(s) with duplicates.", Category.Editor, results.Count);
	}

	private void FixAllDuplicates()
	{
		int fixedCount = 0;
		int skippedCount = 0;

		// Results are already sorted parents-first by ScanForDuplicates.
		for (int i = 0; i < results.Count; i++)
		{
			var info = results[i];
			EditorUtility.DisplayProgressBar("Fixing Duplicates",
				$"{i + 1}/{results.Count}: {info.PrefabPath}", (float)i / results.Count);

			// Re-check after each fix since fixing a parent may resolve child duplicates.
			var go = AssetDatabase.LoadMainAssetAtPath(info.PrefabPath) as GameObject;
			if (go == null) continue;

			bool stillHasDuplicates = info.ComponentType == nameof(Edible)
				? CountExactComponents(go, new List<Edible>()) > 1
				: CountExactComponents(go, new List<ReagentContainer>()) > 1;

			if (stillHasDuplicates == false)
			{
				Loggy.Info().Format("Skipping {0} — duplicate already resolved by parent fix.", Category.Editor, info.PrefabPath);
				skippedCount++;
				continue;
			}

			bool fixed_ = info.ComponentType == nameof(Edible)
				? RemoveDuplicateComponent<Edible>(info.PrefabPath)
				: RemoveDuplicateComponent<ReagentContainer>(info.PrefabPath);

			if (fixed_) fixedCount++;
		}

		EditorUtility.ClearProgressBar();
		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Loggy.Info().Format("Fixed duplicates in {0} prefab(s), {1} resolved by parent fixes.",
			Category.Editor, fixedCount, skippedCount);
		EditorUtility.DisplayDialog("Done",
			$"Fixed duplicates in {fixedCount} prefab(s).\n{skippedCount} were already resolved by parent fixes.", "OK");

		ScanForDuplicates();
	}

	/// <summary>
	/// Walks the source chain for a component and returns how many levels deep it goes.
	/// A component added at the current prefab level returns 0.
	/// </summary>
	private static int GetComponentAncestryDepth(Component comp)
	{
		int depth = 0;
		var current = comp;

		while (current != null)
		{
			var source = PrefabUtility.GetCorrespondingObjectFromSource(current);
			if (source == null || source == current) break;
			depth++;
			current = source;
		}

		return depth;
	}

	private static bool RemoveDuplicateComponent<T>(string prefabPath) where T : Component
	{
		var prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
		if (prefabRoot == null) return false;

		try
		{
			var allOfType = prefabRoot.GetComponents<T>();
			var components = new List<T>();
			foreach (var comp in allOfType)
			{
				if (comp.GetType() == typeof(T)) components.Add(comp);
			}
			if (components.Count <= 1) return false;

			// Find the component with the deepest ancestry (oldest ancestor) and remove it.
			Component toRemove = components[0];
			int deepestAncestry = GetComponentAncestryDepth(components[0]);

			for (int i = 1; i < components.Count; i++)
			{
				int ancestry = GetComponentAncestryDepth(components[i]);
				if (ancestry > deepestAncestry)
				{
					deepestAncestry = ancestry;
					toRemove = components[i];
				}
			}

			Loggy.Info().Format("Removing duplicate {0} (ancestry depth {1}) from {2}",
				Category.Editor, typeof(T).Name, deepestAncestry, prefabPath);
			Object.DestroyImmediate(toRemove, true);

			PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
			return true;
		}
		finally
		{
			PrefabUtility.UnloadPrefabContents(prefabRoot);
		}
	}
}

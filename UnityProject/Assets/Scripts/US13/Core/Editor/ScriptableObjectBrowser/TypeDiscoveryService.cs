using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace US13.Core.Editor.ScriptableObjectBrowser
{
	/// <summary>
	/// A node in the namespace category tree.
	/// </summary>
	public class CategoryNode
	{
		public string Name;
		public string FullPath;
		public List<CategoryNode> Children = new List<CategoryNode>();
		public List<Type> Types = new List<Type>();
		public int TotalTypeCount;
		public bool IsExpanded = true;

		public CategoryNode(string name, string fullPath)
		{
			Name = name;
			FullPath = fullPath;
		}
	}

	/// <summary>
	/// Discovers all concrete ScriptableObject subclasses via TypeCache,
	/// organizes them into a namespace tree, and filters by assembly.
	/// </summary>
	public class TypeDiscoveryService
	{
		private List<Type> allTypes;
		private List<Type> filteredTypes;
		private CategoryNode rootNode;
		private HashSet<string> gameAssemblyNames;
		private bool showGameAssemblies = true;
		private bool showThirdPartyAssemblies = false;

		public List<Type> FilteredTypes => filteredTypes;
		public CategoryNode RootNode => rootNode;
		public bool ShowGameAssemblies
		{
			get => showGameAssemblies;
			set { showGameAssemblies = value; Rebuild(); }
		}
		public bool ShowThirdPartyAssemblies
		{
			get => showThirdPartyAssemblies;
			set { showThirdPartyAssemblies = value; Rebuild(); }
		}

		public TypeDiscoveryService(bool showGame = true, bool showThirdParty = false)
		{
			showGameAssemblies = showGame;
			showThirdPartyAssemblies = showThirdParty;
			BuildGameAssemblySet();
			Refresh();
		}

		/// <summary>
		/// Re-scan all types from TypeCache. Call on domain reload.
		/// </summary>
		public void Refresh()
		{
			allTypes = TypeCache.GetTypesDerivedFrom<ScriptableObject>()
				.Where(t => t.IsAbstract == false
							&& t.IsGenericTypeDefinition == false
							&& typeof(UnityEditor.Editor).IsAssignableFrom(t) == false
							&& typeof(EditorWindow).IsAssignableFrom(t) == false)
				.OrderBy(t => t.Name)
				.ToList();

			Rebuild();
		}

		/// <summary>
		/// Rebuild filtered list and category tree from current filter settings.
		/// </summary>
		private void Rebuild()
		{
			filteredTypes = allTypes.Where(t => IsAssemblyVisible(t.Assembly)).ToList();
			rootNode = BuildCategoryTree(filteredTypes);
		}

		/// <summary>
		/// Get types matching a fuzzy query within an optional namespace filter.
		/// </summary>
		public List<(Type type, int score)> Search(string query, string namespaceFilter = null)
		{
			var source = filteredTypes.AsEnumerable();

			if (string.IsNullOrEmpty(namespaceFilter) == false)
			{
				source = source.Where(t =>
					(t.Namespace ?? "").StartsWith(namespaceFilter, StringComparison.OrdinalIgnoreCase));
			}

			var results = new List<(Type type, int score)>();
			foreach (var type in source)
			{
				int score = FuzzyMatcher.Score(type.Name, query);
				if (score > 0)
				{
					results.Add((type, score));
				}
			}

			results.Sort((a, b) => b.score.CompareTo(a.score));
			return results;
		}

		/// <summary>
		/// Get all types under a namespace prefix.
		/// </summary>
		public List<Type> GetTypesForNamespace(string namespacePrefix)
		{
			if (string.IsNullOrEmpty(namespacePrefix))
				return filteredTypes;

			return filteredTypes
				.Where(t => (t.Namespace ?? "").StartsWith(namespacePrefix, StringComparison.OrdinalIgnoreCase))
				.ToList();
		}

		public bool IsGameAssembly(Assembly assembly)
		{
			return gameAssemblyNames.Contains(assembly.GetName().Name);
		}

		private bool IsAssemblyVisible(Assembly assembly)
		{
			bool isGame = IsGameAssembly(assembly);
			if (isGame && showGameAssemblies) return true;
			if (isGame == false && showThirdPartyAssemblies) return true;
			return false;
		}

		private void BuildGameAssemblySet()
		{
			gameAssemblyNames = new HashSet<string>
			{
				"Assembly-CSharp",
				"Assembly-CSharp-Editor"
			};

			// Find all asmdef assets under the project's script folders
			var asmdefGuids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset",
				new[] { "Assets/Scripts/US13", "Assets/Scripts/Shared", "Assets/ScriptableObjects" });

			foreach (var guid in asmdefGuids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
				if (asset != null)
				{
					// asmdef is JSON with a "name" field
					var wrapper = JsonUtility.FromJson<AsmdefName>(asset.text);
					if (string.IsNullOrEmpty(wrapper.name) == false)
					{
						gameAssemblyNames.Add(wrapper.name);
					}
				}
			}
		}

		private CategoryNode BuildCategoryTree(List<Type> types)
		{
			var root = new CategoryNode("All", "");

			foreach (var type in types)
			{
				string ns = type.Namespace ?? "Global";
				string[] parts = ns.Split('.');
				var current = root;

				string pathSoFar = "";
				foreach (var part in parts)
				{
					pathSoFar = string.IsNullOrEmpty(pathSoFar) ? part : pathSoFar + "." + part;
					var child = current.Children.Find(c => c.Name == part);
					if (child == null)
					{
						child = new CategoryNode(part, pathSoFar);
						current.Children.Add(child);
					}
					current = child;
				}

				current.Types.Add(type);
			}

			// Calculate total counts recursively
			CalculateCounts(root);

			// Sort children alphabetically
			SortTree(root);

			return root;
		}

		private int CalculateCounts(CategoryNode node)
		{
			int count = node.Types.Count;
			foreach (var child in node.Children)
			{
				count += CalculateCounts(child);
			}
			node.TotalTypeCount = count;
			return count;
		}

		private void SortTree(CategoryNode node)
		{
			node.Children.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
			foreach (var child in node.Children)
			{
				SortTree(child);
			}
		}

		[Serializable]
		private class AsmdefName
		{
			public string name;
		}
	}
}

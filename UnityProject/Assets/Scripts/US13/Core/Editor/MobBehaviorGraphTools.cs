using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Behavior;
using UnityEditor;
using UnityEngine;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.SettingsExtensions;
using US13.Mobs.BrainAI.Actions;
using US13.Player;
using US13.Systems.Inventory;

namespace US13.Core.Editor
{
	/// <summary>Rebuild the committed authoring graphs with the installed Behavior package.</summary>
	public static class MobBehaviorGraphTools
	{
		public const string GraphFolder = "Assets/ScriptableObjects/Ai/BehaviorGraphs";

		[MenuItem("Tools/Mobs/Rebuild and Validate Behavior Graphs")]
		public static void RebuildAndValidate()
		{
			var paths = AssetDatabase.FindAssets("t:BehaviorAuthoringGraph", new[] { GraphFolder })
				.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
			if (paths.Length == 0) throw new InvalidOperationException("No mob authoring graphs were imported.");
			// Preflight every asset before building: Unity can strip unresolved nodes during a rebuild.
			foreach (var path in paths) ValidateGraphTypes(path);
			foreach (var path in OrderByDependencies(paths))
			{
				var authoring = AssetDatabase.LoadAllAssetsAtPath(path).Single(asset => asset.GetType().Name == "BehaviorAuthoringGraph");
				AssetDatabase.SetMainObject(authoring, path);
				if (SerializationUtility.HasManagedReferencesWithMissingTypes(authoring)) throw new InvalidOperationException($"Missing node types in {path}");
				// The 1.0 package's authoring type is internal. Keep this version-specific editor bridge out of runtime code.
				var build = authoring.GetType().GetMethod("BuildRuntimeGraph");
				if (build == null) throw new InvalidOperationException("Unsupported Unity Behavior authoring API.");
				var graph = build.Invoke(authoring, new object[] { true }) as BehaviorGraph;
				if (graph == null || SerializationUtility.HasManagedReferencesWithMissingTypes(graph)) throw new InvalidOperationException($"Unable to build {path}");
				EditorUtility.SetDirty(authoring);
				EditorUtility.SetDirty(graph);
			}
			foreach (var path in paths) ValidateGraphTypes(path);
			int brains = 0;
			foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items/Implants/Slime", "Assets/Prefabs/Items/Implants/Organs/Special Organs", "Assets/Prefabs/Mobs/Bot/Resources", "Assets/Prefabs/Mobs/Living/Alien/Arial" }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null) continue;
				foreach (var host in prefab.GetComponentsInChildren<BodyPartBehaviors>(true))
				{
					if (SerializationUtility.HasManagedReferencesWithMissingTypes(host)) throw new InvalidOperationException($"Missing behavior type in {path}");
					foreach (var ai in host.Behaviors.OfType<BrainMobAI>())
					{
						if (ai.Graph == null) throw new InvalidOperationException($"Missing brain graph in {path}");
						ValidateContextExtensions(ai, path);
						ValidateSettings(ai, path);
						brains++;
					}
				}
			}
			if (brains < 28) throw new InvalidOperationException($"Expected at least 28 migrated brains, found {brains}.");
			ValidateSpawnReferences();
			AssetDatabase.SaveAssets();
			Debug.Log($"Mob behavior validation passed: {paths.Length} graphs and {brains} brain configurations.");
		}

		[MenuItem("Tools/Mobs/Configure Required Context Extensions")]
		public static void ConfigureContextExtensions()
		{
			var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Items/Implants/Slime", "Assets/Prefabs/Items/Implants/Organs/Special Organs", "Assets/Prefabs/Mobs/Bot/Resources", "Assets/Prefabs/Mobs/Living/Alien/Arial" })
				.Select(AssetDatabase.GUIDToAssetPath).OrderBy(path => path).ToArray();
			int changed = 0;
			// Mutation tables reference other slime prefabs cyclically. Only inheritance determines migration order.
			foreach (var path in paths.OrderBy(path => PrefabInheritanceDepth(AssetDatabase.LoadAssetAtPath<GameObject>(path))).ThenBy(path => path))
			{
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (!prefab.GetComponentsInChildren<BodyPartBehaviors>(true).Any(host => host.Behaviors.OfType<BrainMobAI>().Any())) continue;
				var root = PrefabUtility.LoadPrefabContents(path);
				try
				{
					bool modified = false;
					foreach (var host in root.GetComponentsInChildren<BodyPartBehaviors>(true))
					{
						using var serialized = new SerializedObject(host);
						var behaviors = serialized.FindProperty("Behaviors");
						for (int i = 0; i < host.Behaviors.Count; i++)
						{
							if (host.Behaviors[i] is not BrainMobAI ai || ai.Graph == null) continue;
							var selections = behaviors.GetArrayElementAtIndex(i).FindPropertyRelative("ContextExtensions");
							foreach (var required in RequiredContextExtensions(ai.Graph))
							{
								if (ai.ContextExtensions.Any(required.IsInstanceOfType)) continue;
								selections.InsertArrayElementAtIndex(selections.arraySize);
								selections.GetArrayElementAtIndex(selections.arraySize - 1).managedReferenceValue = Activator.CreateInstance(required);
								modified = true;
							}
						}
						serialized.ApplyModifiedPropertiesWithoutUndo();
					}
					if (modified) { PrefabUtility.SaveAsPrefabAsset(root, path); changed++; }
				}
				finally { PrefabUtility.UnloadPrefabContents(root); }
			}
			RebuildAndValidate();
			Debug.Log($"Configured context extensions on {changed} brain prefabs.");
		}

		private static int PrefabInheritanceDepth(GameObject prefab)
		{
			int depth = 0;
			while ((prefab = PrefabUtility.GetCorrespondingObjectFromSource(prefab)) != null) depth++;
			return depth;
		}

		public static void ValidateContextExtensions(BrainMobAI ai, string path)
		{
			try
			{
				// Construct runtime data to validate factories as well as inspector selections.
				_ = new MobBehaviorContext(ai);
				foreach (var required in RequiredContextExtensions(ai.Graph))
					if (!ai.ContextExtensions.Any(required.IsInstanceOfType))
						throw new InvalidOperationException($"Graph '{ai.Graph.name}' requires {required.FullName}");
			}
			catch (InvalidOperationException error)
			{
				throw new InvalidOperationException($"Invalid mob context configuration in {path}: {error.Message}", error);
			}
		}

		private static IEnumerable<Type> RequiredContextExtensions(BehaviorGraph graph)
		{
			return GraphActions(graph).SelectMany(action => action.GetType().GetCustomAttributes(typeof(RequiresMobContextExtensionAttribute), true)
				.Cast<RequiresMobContextExtensionAttribute>()).Select(attribute => attribute.ExtensionType).Distinct().OrderBy(type => type.FullName).ToArray();
		}

		public static IEnumerable<Type> RequiredSettingsTypes(BrainMobAI ai)
		{
			var consumers = GraphActions(ai.Graph).Cast<object>().Concat(ai.ContextExtensions ?? new List<IMobBehaviorContextExtension>());
			return consumers.Where(consumer => consumer != null).SelectMany(consumer => consumer.GetType().GetCustomAttributes(typeof(RequiresMobSettingsAttribute), true)
				.Cast<RequiresMobSettingsAttribute>()).Select(attribute => attribute.SettingsType).Distinct().OrderBy(type => type.FullName).ToArray();
		}

		public static void ValidateSettings(BrainMobAI ai, string path)
		{
			try
			{
				if (ai.Settings == null) throw new InvalidOperationException("Shared behavior settings are missing.");
				if (ai.SettingsExtensions == null) throw new InvalidOperationException("The settings extension list is missing.");
				var types = new HashSet<Type>();
				var intervals = new HashSet<int>();
				foreach (var selection in ai.SettingsExtensions)
				{
					if (selection == null) throw new InvalidOperationException("Null settings extension selection.");
					if (!types.Add(selection.GetType())) throw new InvalidOperationException($"Duplicate settings extension {selection.GetType().FullName}");
					if (selection is IMobBehaviorIntervalSettings provider && (provider.IntervalId < 3 || !intervals.Add(provider.IntervalId)))
						throw new InvalidOperationException($"Conflicting settings interval {provider.IntervalId}");
				}
				foreach (var required in RequiredSettingsTypes(ai))
					if (!ai.SettingsExtensions.Any(required.IsInstanceOfType))
						throw new InvalidOperationException($"Graph '{ai.Graph.name}' requires settings {required.FullName}");
				foreach (var wait in GraphActions(ai.Graph).OfType<WaitMobInterval>())
					if (!ai.TryGetInterval(wait.Interval.Value, out _))
						throw new InvalidOperationException($"Missing settings provider for graph interval {wait.Interval.Value}");
			}
			catch (InvalidOperationException error)
			{
				throw new InvalidOperationException($"Invalid mob settings in {path}: {error.Message}", error);
			}
		}

		private static IEnumerable<BrainMobState> GraphActions(BehaviorGraph graph)
		{
			if (graph == null) throw new InvalidOperationException("Missing brain graph.");
			using var serialized = new SerializedObject(graph);
			var property = serialized.GetIterator();
			var visited = new HashSet<long>();
			bool enterChildren = true;
			while (property.Next(enterChildren))
			{
				enterChildren = property.propertyType != SerializedPropertyType.ManagedReference || visited.Add(property.managedReferenceId);
				if (property.propertyType != SerializedPropertyType.ManagedReference || property.managedReferenceValue is not BrainMobState action) continue;
				yield return action;
			}
		}

		/// <summary>Check both Unity managed references and Behavior's assembly-qualified authoring strings.</summary>
		public static void ValidateGraphTypes(string path)
		{
			var assets = AssetDatabase.LoadAllAssetsAtPath(path);
			if (assets.Length == 0) throw new InvalidOperationException($"Missing behavior graph at {path}");
			foreach (var asset in assets)
			{
				if (SerializationUtility.HasManagedReferencesWithMissingTypes(asset))
				{
					var types = SerializationUtility.GetManagedReferencesWithMissingTypes(asset)
						.Select(type => $"{type.namespaceName}.{type.className}, {type.assemblyName}");
					throw new InvalidOperationException($"Missing managed-reference types in {path}/{asset.name}: {string.Join("; ", types)}");
				}
				using var serialized = new SerializedObject(asset);
				var property = serialized.GetIterator();
				var visited = new HashSet<long>();
				bool enterChildren = true;
				while (property.Next(enterChildren))
				{
					// Graph nodes reference their parents and children; do not revisit circular references.
					enterChildren = property.propertyType != SerializedPropertyType.ManagedReference || visited.Add(property.managedReferenceId);
					if (property.propertyType == SerializedPropertyType.String &&
					    (property.name == "m_SerializableType" || property.name == "RuntimeTypeString"))
					{
						string type = property.stringValue;
						if (!string.IsNullOrEmpty(type) && Type.GetType(type, false) == null)
							throw new InvalidOperationException($"Unresolved type '{type}' in {path}/{asset.name} at {property.propertyPath}");
					}
					if (property.propertyType == SerializedPropertyType.Boolean && property.name == "IsPlaceholder" && property.boolValue)
						throw new InvalidOperationException($"Placeholder node in {path}/{asset.name} at {property.propertyPath}. Restore its type before rebuilding.");
				}
				if (asset is BehaviorGraph)
				{
					var modules = serialized.FindProperty("Graphs");
					if (modules == null || modules.arraySize == 0)
						throw new InvalidOperationException($"Missing runtime graph modules in {path}");
					for (int i = 0; i < modules.arraySize; i++)
						if (modules.GetArrayElementAtIndex(i).FindPropertyRelative("Root").managedReferenceValue == null)
							throw new InvalidOperationException($"Missing runtime root in {path}, module {i}");
				}
			}
		}

		private static IEnumerable<string> OrderByDependencies(string[] paths)
		{
			var known = new HashSet<string>(paths);
			var visited = new HashSet<string>();
			var visiting = new HashSet<string>();
			var ordered = new List<string>();
			foreach (var path in paths) Visit(path);
			return ordered;

			void Visit(string path)
			{
				if (visited.Contains(path)) return;
				if (!visiting.Add(path)) throw new InvalidOperationException($"Circular mob graph dependency at {path}");
				foreach (var dependency in AssetDatabase.GetDependencies(path, false).Where(dependency => dependency != path && known.Contains(dependency)).OrderBy(dependency => dependency))
					Visit(dependency);
				visiting.Remove(path);
				visited.Add(path);
				ordered.Add(path);
			}
		}

		/// <summary>Validate the saved references used by character creation, not just prefab lookup by path.</summary>
		public static void ValidateSpawnReferences()
		{
			foreach (var guid in AssetDatabase.FindAssets("t:PlayerHealthData", new[] { "Assets/Prefabs/Player/Resources" }))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var species = AssetDatabase.LoadAssetAtPath<PlayerHealthData>(path);
				if (species?.Base == null) throw new InvalidOperationException($"Missing species configuration in {path}");
				CheckParts(species.Base.Head, "Head", path);
				CheckParts(species.Base.Torso, "Torso", path);
				CheckParts(species.Base.ArmLeft, "ArmLeft", path);
				CheckParts(species.Base.ArmRight, "ArmRight", path);
				CheckParts(species.Base.LegLeft, "LegLeft", path);
				CheckParts(species.Base.LegRight, "LegRight", path);
			}
			foreach (var name in new[] { "FloorBotBody", "CleanBotBody", "FireBotBody", "MedBotBody", "SecuritronBody" })
			{
				var path = $"Assets/Prefabs/Mobs/Bot/Resources/Bodies/{name}.prefab";
				var body = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				var storage = body != null ? body.GetComponent<ItemStorage>() : null;
				var contents = storage?.Populater?.GetFirstLayerDeprecatedAndNew();
				if (contents == null || !contents.Any(prefab => prefab != null && prefab.GetBodyPartBehavior<BrainMobAI>()?.Graph != null))
					throw new InvalidOperationException($"Missing embedded brain/controller reference in {path}");
			}
		}

		private static void CheckParts(ObjectList parts, string slot, string path)
		{
			if (parts == null) return;
			if (parts.Elements == null) throw new InvalidOperationException($"Missing {slot} list in {path}");
			for (int i = 0; i < parts.Elements.Count; i++)
				if (parts.Elements[i] == null)
					throw new InvalidOperationException($"Missing saved body-part reference at Base.{slot}.Elements[{i}] in {path}");
		}
	}
}

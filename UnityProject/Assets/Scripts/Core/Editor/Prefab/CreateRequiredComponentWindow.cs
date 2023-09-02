using System;
using System.Collections.Generic;
using System.Linq;
using Logs;
using UnityEditor;
using UnityEngine;
using Mirror;
using Object = UnityEngine.Object;

/// <summary>
/// Editor window which provides a prompt for entering a component type name.
///
/// The script loads the component type and checks for any RequireComponent attributes
/// on that type or its ancestor types.
///
/// The script will then go through all prefabs and find objects within those prefabs which contain
/// that component, and add all of the components indicated by the RequireComponent attributes identified earlier.
/// </summary>
public class CreateRequiredComponentWindow : EditorWindow
{
	private string componentTypeName;
	private Type[] requiredComponents;
	private List<PrefabModificationInfo> prefabModifications;
	private Vector2 scrollPosition;

	[MenuItem("Prefabs/Create Required Components")]
	static void ShowWindow()
	{
		GetWindow<CreateRequiredComponentWindow>();
	}

	private void OnGUI()
	{
		EditorStyles.label.wordWrap = true;
		EditorGUILayout.LabelField("This will find all prefabs with objects that contain a component of the specified type and" +
		                           " create any required components (indicated by the RequireComponent attribute)" +
		                           " that don't exist on the object.");
		var prevText = componentTypeName;
		componentTypeName = EditorGUILayout.TextField("Component Type Name", componentTypeName);
		if (prevText != componentTypeName && (requiredComponents != null || prefabModifications != null))
		{
			//text changed, clear out stuff
			requiredComponents = null;
			prefabModifications = null;
		}

		if (string.IsNullOrEmpty(componentTypeName))
		{
			GUI.enabled = false;
		}
		if (GUILayout.Button("Scan"))
		{
			Scan();
		}

		//display scan results if there are any
		if (requiredComponents != null && requiredComponents.Length > 0)
		{
			string componentString = string.Join(", ", requiredComponents.Select(rc => rc.Name).ToArray() );
			EditorGUILayout.LabelField("Required components to add: " + componentString);
		}

		if (prefabModifications != null && prefabModifications.Count > 0)
		{
			EditorGUILayout.LabelField("Modifications to perform:");
			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true);

			foreach (var prefabModification in prefabModifications)
			{
				if (prefabModification.Skip) continue;

				if (GUILayout.Button("Skip " + prefabModification.GOToModify.name))
				{
					prefabModification.Skip = true;
				}

				EditorGUILayout.LabelField("in " + AssetDatabase.GUIDToAssetPath(prefabModification.PrefabGUID) + "->" +
				                           prefabModification.GOToModify.name + ":");


				foreach (var componentToAdd in prefabModification.ComponentsToAdd)
				{
					EditorGUILayout.LabelField("\tAdding " + componentToAdd.Name);
				}


			}

			GUILayout.EndScrollView();

			if (GUILayout.Button("Perform modifications"))
			{
				OnCreateRequiredComponents();
			}
		}
	}

	private void Scan()
	{
		if (string.IsNullOrEmpty(componentTypeName))
		{
			EditorUtility.DisplayDialog("Unable to create required components",
				"Please enter a component type name (such as UniversalObjectPhysics, RegisterTile, etc...).", "Close");
			return;
		}

		var types = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(s => s.GetTypes())
			.Where(p => typeof(Component).IsAssignableFrom(p))
			.Where(p => p.Name.Equals(componentTypeName))
			.ToList();

		if (types.Count == 0)
		{
			EditorUtility.DisplayDialog("Unable to create required components",
				$"Could not find component type named {componentTypeName}. Please ensure this is" +
				$" correctly spelled and is a component.", "Close");
			return;
		}

		var componentType = types[0];
		requiredComponents = GetRequiredComponents(componentType).ToArray();

		if (requiredComponents.Length == 0)
		{
			EditorUtility.DisplayDialog("Unable to create required components",
				$"Could not find any required components for component type named {componentTypeName} or its ancestors. Please ensure this is" +
				$" correctly spelled and is a component and it has a RequireComponent attribute in the type or its ancestors.", "Close");
			return;
		}

		var prefabGUIDS = AssetDatabase.FindAssets("t:Prefab");
		prefabModifications = new List<PrefabModificationInfo>();
		foreach (var prefabGUID in prefabGUIDS)
		{
			var path = AssetDatabase.GUIDToAssetPath(prefabGUID);
			var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);

			//find the root gameobject
			var rootPrefabGO = GetRootPrefabGOFromAssets(toCheck);

			if (rootPrefabGO == null)
			{
				continue;
			}

			//does component exist in it or any children?

			if (rootPrefabGO.GetComponentsInChildren(componentType) == null)
			{
				//this GO doesn't have the component that was entered.
				continue;
			}

			//Find all objects that have the component but not all required components
			//check each object under this prefab's hierarchy using BFS
			Queue<GameObject> toExplore = new Queue<GameObject>();
			toExplore.Enqueue(rootPrefabGO);

			while (toExplore.Count > 0)
			{
				var goToCheck = toExplore.Dequeue();
				if (goToCheck.GetComponent(componentType) != null)
				{
					var componentsToAdd = new List<Type>();
					foreach (var requiredComponent in requiredComponents)
					{
						if (goToCheck.GetComponent(requiredComponent) == null)
						{
							componentsToAdd.Add(requiredComponent);
						}
					}

					if (componentsToAdd.Count > 0)
					{
						prefabModifications.Add(new PrefabModificationInfo(prefabGUID, goToCheck, rootPrefabGO,
							componentsToAdd));
					}
				}
			}
		}
	}

	private GameObject GetRootPrefabGOFromAssets(Object[] assetsToCheck)
	{
		foreach (var asset in assetsToCheck)
		{
			if (asset is GameObject)
			{
				var assetGO = asset as GameObject;
				if (assetGO.transform.root == assetGO.transform)
				{
					return assetGO;
				}
			}
		}

		return null;
	}

	private void OnCreateRequiredComponents()
	{
		Loggy.Log("Performing modifications:", Category.Editor);
		foreach (var prefabModification in prefabModifications)
		{
			if (prefabModification.Skip) continue;
			var path = AssetDatabase.GUIDToAssetPath(prefabModification.PrefabGUID);
			foreach (var componentToAdd in prefabModification.ComponentsToAdd)
			{
				prefabModification.GOToModify.AddComponent(componentToAdd);

				PrefabUtility.SavePrefabAsset(prefabModification.RootGO);

				Loggy.LogFormat("Added {0} to game object {1} in prefab {2}", Category.Editor,
					componentToAdd.Name, prefabModification.GOToModify, path);
			}
		}

		prefabModifications.Clear();

		EditorUtility.DisplayDialog("Complete",
			"Done creating required components. Check console for details.", "Close");
	}


	private IEnumerable<Type> GetRequiredComponents(Type componentType)
	{
		RequireComponent[] requireComponents = (RequireComponent[]) componentType.GetCustomAttributes(typeof(RequireComponent), true);

		var result = new List<Type>();

		foreach (var requireComponent in requireComponents)
		{
			if (requireComponent.m_Type0 != null)
			{
				result.Add(requireComponent.m_Type0);
			}
			if (requireComponent.m_Type1 != null)
			{
				result.Add(requireComponent.m_Type1);
			}
			if (requireComponent.m_Type2 != null)
			{
				result.Add(requireComponent.m_Type2);
			}
		}

		//exclude netidentity
		return result.Where(t => !typeof(NetworkIdentity).IsAssignableFrom(t));
	}

	private static bool IsPrefab(GameObject toCheck) => !toCheck.transform.gameObject.scene.IsValid();

	private class PrefabModificationInfo
	{
		//GUID referencing the prefab this GO is in
		public string PrefabGUID;
		//root GO of the prefab
		public GameObject RootGO;
		//GO within this prefab which will be modified (might not always be the root)
		public GameObject GOToModify;
		//components which will be added to this GO
		public List<Type> ComponentsToAdd;
		//skipping
		public bool Skip;

		public PrefabModificationInfo(string prefabGuid, GameObject goToModify, GameObject rootGO, List<Type> componentsToAdd)
		{
			PrefabGUID = prefabGuid;
			GOToModify = goToModify;
			RootGO = rootGO;
			ComponentsToAdd = componentsToAdd;
		}
	}
}

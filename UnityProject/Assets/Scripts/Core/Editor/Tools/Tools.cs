﻿using System;
using System.Collections.Generic;
using System.Linq;
using Systems.CraftingV2;
using Mirror;
using Objects.Wallmounts;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;
using Object = UnityEngine.Object;


namespace Core.Editor.Tools
{
	public class Tools : UnityEditor.Editor
	{
		class Conn
		{
			public Vector3 worldPos = Vector3.zero;
			public Connection wireEndA = Connection.East;
			public Connection wireEndB = Connection.East;
			public PowerTypeCategory wireType = PowerTypeCategory.Transformer;
		}

		[MenuItem("Mapping/Refresh Directionals")]
		private static void RefreshDirectionals()
		{
			var allDirs = FindObjectsOfType<Directional>();
			foreach (var directional in allDirs)
			{
				directional.ChangeDirectionInEditor();
			}
			Logger.Log($"Refreshed {allDirs.Length} directionals", Category.Editor);
		}

		[MenuItem("Mapping/Set all sceneids to 0")]
		private static void SetAllSceneIdsToNull()
		{
			var allNets = FindObjectsOfType<NetworkIdentity>();

			for (int i = allNets.Length - 1; i > 0; i--)
			{
				allNets[i].sceneId = 0;
				EditorUtility.SetDirty(allNets[i]);
			}

			Logger.Log($"Set {allNets.Length} scene ids", Category.Editor);
		}

		[MenuItem("Networking/Find all network identities without visibility component (Scene Check)")]
		private static void FindNetWithoutVis()
		{
			var allNets = FindObjectsOfType<NetworkIdentity>();

			for (int i = allNets.Length - 1; i > 0; i--)
			{
				var net = allNets[i].GetComponent<CustomNetSceneChecker>();

				if (net == null)
				{
					Debug.Log($"{allNets[i].name} prefab has no visibility component");
				}
			}

			Debug.Log($"{allNets.Length} net components found in the scene");
		}

		[MenuItem("Networking/Find all network identities without visibility component (Prefab Check)")]
		private static void FindNetWithoutVisScene()
		{
			var allNets = LoadPrefabsContaining<NetworkIdentity>("Assets/Prefabs");

			for (int i = allNets.Count - 1; i > 0; i--)
			{
				var net = allNets[i].GetComponent<CustomNetSceneChecker>();

				if (net == null)
				{
					Debug.Log($"{allNets[i].name} prefab has no visibility component");
				}
			}

			Debug.Log($"{allNets.Count} net components found in prefabs");
		}

		[MenuItem("Networking/Find all asset Ids (Prefab Check)")]
		private static void FindAssetIdsPrefab()
		{
			var allNets = LoadPrefabsContaining<NetworkIdentity>("Assets/Prefabs");

			for (int i = allNets.Count - 1; i > 0; i--)
			{
				var net = allNets[i].GetComponent<NetworkIdentity>();

				if (net.assetId == Guid.Empty)
				{
					Debug.Log($"{allNets[i].name} has empty asset id");
				}
			}

			Debug.Log($"{allNets.Count} net components found in prefabs");
		}

		[MenuItem("Mapping/Save all scenes")]
		private static void SaveAllScenes()
		{
			var scenesGUIDs = AssetDatabase.FindAssets("t:Scene",new string[] {"Assets/Scenes"});
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath);

			foreach (var scene in scenesPaths)
			{
				if (scene.Contains("DevScenes") || scene.StartsWith("Packages")) continue;

				var openScene = EditorSceneManager.OpenScene(scene);

				EditorSceneManager.MarkSceneDirty(openScene);
				EditorSceneManager.SaveScene(openScene);
				EditorSceneManager.CloseScene(openScene, true);
			}
		}

		/// <summary>
		/// Find all prefabs containing a specific component (T)
		/// </summary>
		/// <typeparam name="T">The type of component</typeparam>
		public static List<GameObject> LoadPrefabsContaining<T>(string path) where T : UnityEngine.Component
		{
			List<GameObject> result = new List<GameObject>();

			var networkObjectsGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {path});
			var objectsPaths = networkObjectsGUIDs.Select(AssetDatabase.GUIDToAssetPath);
			foreach (var objectsPath in objectsPaths)
			{
				var obj = AssetDatabase.LoadAssetAtPath<GameObject>(objectsPath);
				if (obj != null && obj.GetComponent<T>() != null)
				{
					result.Add(obj);
				}
			}
			return result;
		}

		[MenuItem("Networking/Check for duplicate net Ids (Scene Check)")]
		private static void FindNetIds()
		{
			var allNets = FindObjectsOfType<NetworkIdentity>();

			var netIds = new HashSet<uint>();

			for (int i = allNets.Length - 1; i > 0; i--)
			{
				var net = allNets[i].GetComponent<NetworkIdentity>();

				if (netIds.Add(net.netId) == false)
				{
					Debug.Log($"{allNets[i]} has a duplicate net Id");
				}
			}

			Debug.Log($"{allNets.Length} net ids found in the scene");
		}

		//this is just for migrating from old way of setting wallmount directions to the new way
		[MenuItem("Tools/Set Wallmount Directionals from Transforms")]
		private static void FixWallmountDirectionals()
		{
			foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				foreach (var wallmount in gameObject.GetComponentsInChildren<WallmountBehavior>())
				{
					var directional = wallmount.GetComponent<Directional>();
					var directionalSO = new SerializedObject(directional);
					var initialD = directionalSO.FindProperty("InitialDirection");

					Vector3 facing = -wallmount.transform.up;
					var initialOrientation = Orientation.From(facing);
					initialD.enumValueIndex = (int) initialOrientation.AsEnum();
					directionalSO.ApplyModifiedPropertiesWithoutUndo();
				}
			}
		}

		//this is just for migrating from old way of setting wall protrusion directions to the new way
		[MenuItem("Tools/Set WallProtrusion Directionals from Transforms")]
		private static void FixWallProtrusionDirectionals()
		{
			foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				foreach (var wallProtrusion in gameObject.GetComponentsInChildren<WallProtrusion>())
				{
					var directional = wallProtrusion.GetComponent<Directional>();
					var directionalSO = new SerializedObject(directional);
					var initialD = directionalSO.FindProperty("InitialDirection");

					Vector3 facing = -wallProtrusion.transform.up;
					var initialOrientation = Orientation.From(facing);
					initialD.enumValueIndex = (int) initialOrientation.AsEnum();
					directionalSO.ApplyModifiedPropertiesWithoutUndo();
				}
			}
		}

		/// <summary>
		/// With the new way mapping works, now you should never have a situation where you've
		/// mapped something with a transform rotation, they should always have a local rotation
		/// of 0,0,0, and directional / rotation logic must be set using components.
		///
		/// This is a script for making sure that's the case
		/// </summary>
		[MenuItem("Tools/Set All Object Local Rotations to Upright")]
		private static void SetAllObjectLocalRotationsUpright()
		{
			foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				foreach (var registerTile in gameObject.GetComponentsInChildren<RegisterTile>())
				{
					var transform = new SerializedObject(registerTile.transform);
					var localRotation = transform.FindProperty("m_LocalRotation");
					localRotation.quaternionValue = Quaternion.identity;
					transform.ApplyModifiedPropertiesWithoutUndo();
				}
			}
		}

		//they should always be upright unless they are directional.
		[MenuItem("Tools/Set All non-directional Wallmount Sprite Rotations to Upright")]
		private static void SetAllNonDirectionalWallmountSpriteRotationsUpright()
		{
			foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				foreach (var wallmount in gameObject.GetComponentsInChildren<WallmountBehavior>())
				{
					if (wallmount.GetComponent<DirectionalRotationSprites>() != null) continue;
					foreach (var spriteRenderer in wallmount.GetComponentsInChildren<SpriteRenderer>())
					{
						var transform = new SerializedObject(spriteRenderer.transform);
						var localRotation = transform.FindProperty("m_LocalRotation");
						localRotation.quaternionValue = Quaternion.identity;
						transform.ApplyModifiedPropertiesWithoutUndo();
					}
				}
			}
		}

		//this is for fixing some prefabs that have duplicate Meleeable components
		[MenuItem("Prefabs/Remove Duplicate Meleeable")]
		private static void RemoveDuplicateMeleeable()
		{
			var prefabGUIDS = AssetDatabase.FindAssets("t:Prefab");
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
				var melees = rootPrefabGO.GetComponents<BoxCollider2D>();

				if (melees == null || melees.Length <= 1) continue;

				Logger.LogFormat("Removing duplicate Meleeables from {0}", Category.Editor, rootPrefabGO.name);

				//remove excess
				for (int i = 1; i < melees.Length; i++)
				{
					GameObject.DestroyImmediate(melees[i], true);
				}

				PrefabUtility.SavePrefabAsset(rootPrefabGO);
			}
		}

		private static GameObject GetRootPrefabGOFromAssets(Object[] assetsToCheck)
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

		/// <summary>Snap all allowed objects to the middle of the nearest tile.</summary>
		[MenuItem("Tools/Mapping/Snap to Grid All Applicable Objects")]
		private static void CenterObjects()
		{
			int count = 0;
			foreach (GameObject gameObject in SceneManager.GetActiveScene().GetRootGameObjects())
			{
				foreach (var cnt in gameObject.GetComponentsInChildren<CustomNetTransform>())
				{
					if (cnt.SnapToGridOnStart == false) continue;

					var initialPosition = cnt.transform.position;
					cnt.transform.position = cnt.transform.position.RoundToInt();
					if (cnt.transform.position != initialPosition)
					{
						count++;
					}
				}
			}

			Logger.Log($"Centered {count} objects!");
		}

		[MenuItem("Tools/Remove Missing Scripts")]
		/// Courtesy of <see cref="https://answers.unity.com/questions/15225/how-do-i-remove-null-components-ie-missingmono-scr.html?childToView=1614734#answer-1614734"/>
		private static void RemoveMissingScripts()
		{
			int compCount = 0;
			int goCount = 0;

			foreach (var o in AssetDatabase.FindAssets("t:Prefab")
				.Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))))
			{
				if (o is GameObject go)
				{
					int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
					if (count > 0)
					{
						// Edit: use undo record object, since undo destroy wont work with missing
						Undo.RegisterCompleteObjectUndo(go, "Remove missing scripts");
						GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
						compCount += count;
						goCount++;
					}
				}
			}
			Debug.Log($"Found and removed {compCount} missing scripts from {goCount} GameObjects");
		}

		[MenuItem("Tools/Crafting/FixCraftingCrossLinks")]
		private static void CheckAndFixCraftingCrossLinks()
		{
			// thank unity we have no better way to see variants (heirs) of game objects.
			// This dictionary contains pairs of values:
			// <Parent, List<parent's heirs>> or something like that. Heirs can also have its heirs, so they also
			// can be parents and should be presented in this dictionary with the matching key.
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds =
				FindUtils.BuildAndGetInheritanceDictionaryOfPrefabs(new List<Type> {typeof(CraftingIngredient)});
			string[] recipeGuids = AssetDatabase.FindAssets("t:CraftingRecipe");

			if (recipeGuids.Length == 0)
			{
				return;
			}

			foreach (string recipeGuid in recipeGuids)
			{
				CraftingRecipe recipe = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(
					AssetDatabase.GUIDToAssetPath(recipeGuid)
				);
				for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
				{
					CheckAndFixCraftingCrossLinks(
						recipe,
						i,
						recipe.RequiredIngredients[i].RequiredItem,
						parentsAndChilds
					);
				}
			}
		}

		/// <summary>
		/// 	Checks and fixes cross links of the ingredient-object and all its heirs recursively.
		/// </summary>
		/// <param name="checkingRecipe">What recipe we need to check and fix?</param>
		/// <param name="indexInRecipe">What index should be present in the ingredient?</param>
		/// <param name="requiredIngredient">What ingredient and its heirs we need to check and fix?</param>
		/// <param name="parentsAndChilds">Dictionary of game objects and its heirs(variants).</param>
		private static void CheckAndFixCraftingCrossLinks(
			CraftingRecipe checkingRecipe,
			int indexInRecipe,
			GameObject requiredIngredient,
			Dictionary<GameObject, HashSet<GameObject>> parentsAndChilds
		)
		{
			CraftingIngredient craftingIngredient = requiredIngredient.GetComponent<CraftingIngredient>();
			// has the ingredient a link to the recipe?
			bool foundRecipe = false;
			foreach (RelatedRecipe relatedRecipe in craftingIngredient.RelatedRecipes)
			{
				if (relatedRecipe.Recipe != checkingRecipe)
				{
					continue;
				}

				foundRecipe = true;
				if (relatedRecipe.IngredientIndex != indexInRecipe)
				{
					Logger.Log(
						$"A crafting ingredient ({requiredIngredient}) had a wrong related recipe index, " +
						"but was fixed automatically. " +
						$"Expected {indexInRecipe}, but found: {relatedRecipe.IngredientIndex}."
					);
					relatedRecipe.IngredientIndex = indexInRecipe;
					PrefabUtility.SavePrefabAsset(requiredIngredient);
				}
				break;
			}

			if (foundRecipe == false)
			{
				Logger.Log(
					$"A crafting ingredient ({requiredIngredient}) didn't have a link to a recipe " +
					$"({checkingRecipe}) in its RelatedRecipes list, since the recipe requires this " +
					"ingredient (prefab), any of it's heirs (prefab variants) " +
					"or even some parents (prefab sources aka bases)."
				);
				craftingIngredient.RelatedRecipes.Add(new RelatedRecipe(checkingRecipe, indexInRecipe));
				PrefabUtility.SavePrefabAsset(requiredIngredient);
			}

			if (parentsAndChilds.ContainsKey(requiredIngredient) == false)
			{
				return;
			}

			foreach (GameObject child in parentsAndChilds[requiredIngredient])
			{
				CheckAndFixCraftingCrossLinks(checkingRecipe, indexInRecipe, child, parentsAndChilds);
			}
		}
	}
}

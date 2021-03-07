﻿using System;
using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Objects.Wallmounts;
using Object = UnityEngine.Object;

public class Tools : Editor
{
	class Conn
	{
		public Vector3 worldPos = Vector3.zero;
		public Connection wireEndA = Connection.East;
		public Connection wireEndB = Connection.East;
		public PowerTypeCategory wireType = PowerTypeCategory.Transformer;

	}

	[MenuItem("Tools/Refresh Directionals")]
	private static void RefreshDirectionals()
	{
		var allDirs = FindObjectsOfType<Directional>();

		for (int i = allDirs.Length - 1; i > 0; i--)
		{
			if(allDirs[i].onEditorDirectionChange != null)
				allDirs[i].onEditorDirectionChange.Invoke();

			allDirs[i].transform.localEulerAngles = Vector3.zero;
		}

		Debug.Log($"Refreshed {allDirs.Length} directionals");
	}

	[MenuItem("Networking/Set all sceneids to 0")]
	private static void SetAllSceneIdsToNull()
	{
		var allNets = FindObjectsOfType<NetworkIdentity>();

		for (int i = allNets.Length - 1; i > 0; i--)
		{
			allNets[i].sceneId = 0;
			EditorUtility.SetDirty(allNets[i]);
		}

		Debug.Log($"Set {allNets.Length} scene ids");
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
		var allNets = LoadPrefabsContaining<NetworkIdentity>("Prefabs");

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
    	var allNets = LoadPrefabsContaining<NetworkIdentity>("Prefabs");

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

	/// <summary>
	/// Find all prefabs containing a specific component (T)
	/// </summary>
	/// <typeparam name="T">The type of component</typeparam>
	public static List<GameObject> LoadPrefabsContaining<T>(string path) where T : UnityEngine.Component
	{
		List<GameObject> result = new List<GameObject>();

		var allFiles = Resources.LoadAll<UnityEngine.Object>(path);
		foreach (var obj in allFiles)
		{
			if (obj is GameObject)
			{
				GameObject go = obj as GameObject;
				if (go.GetComponent<T>() != null)
				{
					result.Add(go);
				}
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
}
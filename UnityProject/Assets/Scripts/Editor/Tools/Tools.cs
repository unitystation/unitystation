using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

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
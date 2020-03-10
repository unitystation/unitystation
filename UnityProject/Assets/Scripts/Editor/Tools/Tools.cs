using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Tools : Editor
{
	class Conn
	{
		public Vector3 worldPos;
		public Connection wireEndA;
		public Connection wireEndB;
		public PowerTypeCategory wireType;

	}

	[MenuItem("Tools/Clean Up Wire Dupes")]
	private static void RemoveWireDupes()
	{
		List<Conn> testConns = new List<Conn>();
		var allWires = FindObjectsOfType<WireConnect>();

		int wireDupes = 0;
		for (int i = allWires.Length - 1; i > 0; i--)
		{
			var w = allWires[i];
			var cable = w.GetComponent<CableInheritance>();
			if (cable == null) continue;

			var c = new Conn
			{
				worldPos = w.transform.position,
				wireEndA = w.WireEndA,
				wireEndB = w.WireEndB,
				wireType = cable.ApplianceType
			};

			var index = testConns.FindIndex(x => x.worldPos == c.worldPos &&
			                                     x.wireEndA == c.wireEndA &&
			                                     x.wireEndB == c.wireEndB &&
												 x.wireType == c.wireType);

			if (index == -1)
			{
				testConns.Add(c);
			}
			else
			{
				DestroyImmediate(w.gameObject);
				wireDupes++;
			}
		}

		Debug.Log($"Removed {wireDupes} wire dupes");
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
			var melees = rootPrefabGO.GetComponents<Meleeable>();

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
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;


public class Tools : Editor
	{


		[MenuItem("Tools/Reconnect TileConnect")]
		private static void RevertTileConnect()
		{
			//            var triggers = FindObjectsOfType<ConnectTrigger>();
			//
			//            foreach (var t in triggers)
			//            {
			//                PrefabUtility.RevertPrefabInstance(t.gameObject);
			//            }
		}

		[MenuItem("Tools/Set Ambient Tiles")]
		private static void SetAmbientTiles()
		{
			FloorTile[] tiles = FindObjectsOfType<FloorTile>();

			foreach (FloorTile t in tiles)
			{
				t.CheckAmbientTile();
			}
		}

		[MenuItem("Tools/Revert To Prefab %r")]
		private static void RevertPrefabs()
		{
			GameObject[] selection = Selection.gameObjects;

			if (selection.Length > 0)
			{
				for (int i = 0; i < selection.Length; i++)
				{
					PrefabUtility.RevertPrefabInstance(selection[i]);
					PrefabUtility.ReconnectToLastPrefab(selection[i]);
				}
			}
			else
			{
				Logger.Log("Cannot revert to prefab - nothing selected");
			}
		}

		// TODO replace for new tilemap system
		//		[MenuItem("Tools/Resection Tiles")]
		//		static void ConnectTiles2Sections()
		//		{
		//			var registerTiles = FindObjectsOfType<RegisterTile>();
		//
		//			foreach (var r in registerTiles) {
		//				var p = r.transform.position;
		//
		//				int x = Mathf.RoundToInt(p.x);
		//				int y = Mathf.RoundToInt(p.y);
		//
		//				r.transform.MoveToSection(Matrix.Matrix.At(x, y).Section);
		//				//PrefabUtility.RevertPrefabInstance(r.gameObject);
		//			}
		//		}

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

		//for migrating from Old ItemAttributes to ItemAttributesV2
//		[MenuItem("Prefabs/Migrate Item Attributes")]
//		private static void MigrateItemAttributes()
//		{
//			//scan for prefabs which contain OldItemAttributes
//			var prefabGUIDS = AssetDatabase.FindAssets("t:Prefab");
//			foreach (var prefabGUID in prefabGUIDS)
//			{
//				var path = AssetDatabase.GUIDToAssetPath(prefabGUID);
//				var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
//
//				//find the root gameobject
//				var rootPrefabGO = GetRootPrefabGOFromAssets(toCheck);
//
//				if (rootPrefabGO == null)
//				{
//					continue;
//				}
//				//does component exist in it or any children?
//				if (rootPrefabGO.GetComponent<ItemAttributesV2>() == null)
//				{
//					continue;
//				}
//
//				//Found one that has OldItemAttributes, now create ItemAttributesV2 and migrate the
//				//fields
//				var addedAttributes = rootPrefabGO.AddComponent<ItemAttributesV2>();
//				var oldAttributes = rootPrefabGO.GetComponent<ItemAttributes>();
//				addedAttributes.MigrateFromOld(rootPrefabGO.GetComponent<ItemAttributes>());
//				Logger.Log("Modified " + rootPrefabGO.name);
//				DestroyImmediate(oldAttributes,true);
//				PrefabUtility.SavePrefabAsset(rootPrefabGO);
//			}
//		}
	}

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

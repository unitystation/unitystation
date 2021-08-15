using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Tests.Asset
{
	public class NetworkManagerTests
	{
		[Test]
		public void SpawnableListTest()
		{
			var scenesGUIDs = AssetDatabase.FindAssets("OnlineScene t:Scene");
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToList();

			if (scenesPaths.Count != 1)
			{
				Assert.Fail($"Couldn't find OnlineScene, or more than one OnlineScene found");
				return;
			}

			var openScene = EditorSceneManager.OpenScene(scenesPaths[0]);

			var rootObjects = openScene.GetRootGameObjects();

			var report = new StringBuilder();

			Dictionary<string, PrefabTracker> StoredIDs = new Dictionary<string, PrefabTracker>();

			foreach (var rootObject in rootObjects)
			{
				if (rootObject.TryGetComponent<CustomNetworkManager>(out var manager))
				{
					var failed = false;
					var networkObjectsGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
					var objectsPaths = networkObjectsGUIDs.Select(AssetDatabase.GUIDToAssetPath);
					foreach (var objectsPath in objectsPaths)
					{
						if (objectsPath.Contains("Assets/Prefabs/SceneConstruction/NestedManagers")) continue;

						var asset = AssetDatabase.LoadAssetAtPath<GameObject>(objectsPath);
						if(asset == null) continue;

						if (asset.TryGetComponent<NetworkIdentity>(out _) && manager.spawnPrefabs.Contains(asset) == false  && manager.playerPrefab != asset)
						{
							failed = true;
							report.AppendLine($"{asset} needs to be in the spawnPrefabs list and has been added." +
							            " Since the list has been updated you NEED to commit the changed NetworkManager Prefab file");
						}

						if (manager.allSpawnablePrefabs.Contains(asset) == false)
						{
							failed = true;
							report.AppendLine($"{asset} needs to be in the allSpawnablePrefabs list and has been added." +
							                   " Since the list has been updated you NEED to commit the changed NetworkManager Prefab file");
						}

						if (asset.TryGetComponent<PrefabTracker>(out var prefabTracker))
						{
							if (StoredIDs.ContainsKey(prefabTracker.ForeverID))
							{
								failed = true;
								report.AppendLine($"{prefabTracker} or {StoredIDs[prefabTracker.ForeverID]} NEEDS to be committed with it's new Forever ID ");
							}
							StoredIDs[prefabTracker.ForeverID] = prefabTracker;
						}
					}

					var spawnListMonitor = manager.GetComponent<SpawnListMonitor>();
					if (spawnListMonitor.GenerateSpawnList())
					{
						PrefabUtility.ApplyPrefabInstance(spawnListMonitor.gameObject, InteractionMode.AutomatedAction);
					}

					if (failed)
					{
						Assert.Fail(report.ToString());
					}

					return;
				}
			}
		}
	}
}

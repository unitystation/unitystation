using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Util;

namespace Tests.Asset
{
	public class NetworkManagerTests
	{
		[Test]
		public void SpawnableListTest()
		{
			var scenesGUIDs = AssetDatabase.FindAssets("OnlineScene t:Scene", new string[] { "Assets/Scenes" });
			var scenesPaths = scenesGUIDs.Select(AssetDatabase.GUIDToAssetPath).ToList();

			if (scenesPaths.Count != 1)
			{
				Assert.Fail($"Couldn't find OnlineScene, or more than one OnlineScene found");
				return;
			}

			var openScene = EditorSceneManager.OpenScene(scenesPaths[0]);
			var rootObjects = openScene.GetRootGameObjects();
			var manager = rootObjects.Select(obj => obj.GetComponent<CustomNetworkManager>()).Where(obj => obj != null).FirstOrDefault();
			if (manager == default)
			{
				Assert.Fail($"Couldn't find {nameof(CustomNetworkManager)} in OnlineScene.");
				return;
			}

			var missingNetworkedAssets = new List<GameObject>();
			var missingSpawnableAssets = new List<GameObject>();

			var failed = false;
			var report = new StringBuilder();

			var storedIDs = new Dictionary<string, PrefabTracker>();
			var networkObjectsGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets/Prefabs" });
			var objectsPaths = networkObjectsGUIDs.Select(AssetDatabase.GUIDToAssetPath);
			foreach (var objectsPath in objectsPaths)
			{
				if (objectsPath.Contains("Assets/Prefabs/SceneConstruction/NestedManagers")) continue;

				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(objectsPath);
				if (asset == null) continue;

				if (asset.TryGetComponent<NetworkIdentity>(out _) && manager.spawnPrefabs.Contains(asset) == false && asset != manager.playerPrefab)
				{
					missingNetworkedAssets.Add(asset);
				}

				if (manager.allSpawnablePrefabs.Contains(asset) == false)
				{
					missingSpawnableAssets.Add(asset);
				}

				if (asset.TryGetComponent<PrefabTracker>(out var prefabTracker))
				{
					if (storedIDs.ContainsKey(prefabTracker.ForeverID)) {
						report.AppendLine($"Prefab tracker list will be updated regarding {asset}. " +
							$"The changed NetworkManager prefab NEEDS to be committed with its new Forever IDs.");
						failed = true;
					}

					storedIDs[prefabTracker.ForeverID] = prefabTracker;
				}
			}

			failed |= missingNetworkedAssets.Count > 0 || missingSpawnableAssets.Count > 0;

			var spawnListMonitor = manager.GetComponent<SpawnListMonitor>();
			if (failed && spawnListMonitor.GenerateSpawnList())
			{
				PrefabUtility.ApplyPrefabInstance(spawnListMonitor.gameObject, InteractionMode.AutomatedAction);
			}

			if (missingNetworkedAssets.Count > 0)
			{
				report.AppendLine($"Assets {string.Join(", ", missingNetworkedAssets.Select(obj => obj.name))} weren't " +
						$"in the {nameof(NetworkManager)} {nameof(manager.spawnPrefabs)} list.");
			}

			if (missingSpawnableAssets.Count > 0)
			{
				report.AppendLine($"Assets {string.Join(", ", missingSpawnableAssets.Select(obj => obj.name))} weren't " +
						$"in the {nameof(CustomNetworkManager)} {nameof(manager.allSpawnablePrefabs)} list.");
			}

			if (failed)
			{
				report.AppendLine("If the lists were updated you NEED to commit the changed NetworkManager prefab.");
				Assert.Fail(report.ToString());
			}
		}
	}
}

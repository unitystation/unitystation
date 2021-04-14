using System.Linq;
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
		public void NetworkIdentityTest()
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

			foreach (var rootObject in rootObjects)
			{
				if (rootObject.TryGetComponent<CustomNetworkManager>(out var manager))
				{
					var spawnListMonitor = manager.GetComponent<SpawnListMonitor>();
					if (spawnListMonitor.GenerateSpawnList())
					{
						PrefabUtility.ApplyPrefabInstance(spawnListMonitor.gameObject, InteractionMode.AutomatedAction);
					}

					var networkObjectsGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
					var objectsPaths = networkObjectsGUIDs.Select(AssetDatabase.GUIDToAssetPath);
					foreach (var objectsPath in objectsPaths)
					{
						var asset = AssetDatabase.LoadAssetAtPath<GameObject>(objectsPath);
						if(asset == null) continue;

						if (asset.TryGetComponent<NetworkIdentity>(out _) && manager.spawnPrefabs.Contains(asset) == false)
						{
							Assert.Fail($"{asset} needs to be in the spawnable list, press the fill list button on the NetworkManager");
						}
					}

					return;
				}
			}
		}
	}
}

using System.Linq;
using Mirror;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Util;

namespace Tests.Asset
{
	[Category(nameof(Asset))]
	public class NetworkManagerTests
	{
		[Test]
		public void NetworkManagerHasSpawnListManager()
		{
			var onlineScene = GetOnlineSceneOrThrow();
			var networkManager = GetNetworkManagerOrThrow(onlineScene);
			var report = new TestReport();
			using var idsPool = DictionaryPool<string, PrefabTracker>.Get(out var storedIDs);
			using var prefabPool = ListPool<GameObject>.Get(out var prefabs);

			prefabs.AddRange(Utils.FindPrefabs(pathFilter: s => s.Contains("NestedManagers") == false));

			foreach (var prefab in prefabs)
			{
				report.FailIf(PrefabIsNotInSpawnPrefabs(prefab, networkManager))
					.Append($"{prefab.name} needs to be in the spawnPrefabs list and has been added. ")
					.Append("Since the list has been updated you NEED to commit the changed NetworkManager Prefab file")
					.AppendLine()
					.FailIfNot(networkManager!.allSpawnablePrefabs.Contains(prefab))
					.Append($"{prefab.name} needs to be in the allSpawnablePrefabs list and has been added. ")
					.Append("Since the list has been updated you NEED to commit the changed NetworkManager Prefab file")
					.AppendLine();

				if (prefab.TryGetComponent<PrefabTracker>(out var prefabTracker) == false) continue;

				var foreverID = prefabTracker.ForeverID;
				report.FailIf(storedIDs.TryGetValue(foreverID, out var tracker))
					.AppendLine(
						$"{prefabTracker.name} or {tracker.OrNull()?.name} NEEDS to be committed with it's new Forever ID.");
				storedIDs[prefabTracker.ForeverID] = prefabTracker;
			}

			report.FailIfNot(networkManager!.TryGetComponent<SpawnListMonitor>(out var spawnListMonitor))
				.AppendLine($"{nameof(CustomNetworkManager)} does not contain a {nameof(SpawnListMonitor)}!")
				.AssertPassed();

			if (spawnListMonitor.GenerateSpawnList())
			{
				PrefabUtility.ApplyPrefabInstance(spawnListMonitor.gameObject, InteractionMode.AutomatedAction);
			}

			report.AssertPassed();
		}

		private Scene GetOnlineSceneOrThrow()
		{
			var scenePaths = Utils.GUIDsToPaths(Utils.FindGUIDsOfType("Scene", "Assets/Scenes"),
				s => s.Contains("OnlineScene"))
				.ToList();

			Assert.That(scenePaths.Count, Is.EqualTo(1),
				"OnlineScene doesn't exist or more than one scene named OnlineScene exists.");

			return EditorSceneManager.OpenScene(scenePaths.First());
		}

		private CustomNetworkManager GetNetworkManagerOrThrow(Scene scene)
		{
			var networkManager = scene.GetRootGameObjects()
				.Select(go => go.GetComponent<CustomNetworkManager>())
				.NotNull()
				.FirstOrDefault();

			Assert.That(networkManager, Is.Not.Null, $"{scene.name} does not contain a {nameof(CustomNetworkManager)}!");

			return networkManager;
		}

		private bool PrefabIsNotInSpawnPrefabs(GameObject prefab, CustomNetworkManager networkManager) =>
			prefab.TryGetComponent<NetworkIdentity>(out _)
			&& networkManager!.spawnPrefabs.Contains(prefab) == false
			&& networkManager.playerPrefab != prefab;
	}
}

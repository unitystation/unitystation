using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Tests.Config
{
	public class GameManagerTest
	{
		private const string GAMEMANAGER_PATH = "Assets/Prefabs/SceneConstruction/NestedManagers";

		[Test]
		public void CheckQuickLoad()
		{
			var gameManagerPrefabGUID = AssetDatabase.FindAssets("GameManager t:prefab", new string[] {GAMEMANAGER_PATH});
			var gameManagerPrefabPaths = gameManagerPrefabGUID.Select(AssetDatabase.GUIDToAssetPath).ToList();

			if (gameManagerPrefabPaths.Count != 1)
			{
				Assert.Fail($"Couldn't find GameManager prefab in specified path, or more than one game manager found at: {GAMEMANAGER_PATH}");
				return;
			}

			var gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameManagerPrefabPaths.First());
			if (gameManagerPrefab == null)
			{
				Assert.Fail($"Couldn't find GameManager prefab in specified path: {GAMEMANAGER_PATH}");
			}

			if (gameManagerPrefab.TryGetComponent<GameManager>(out var gameManager) == false)
			{
				Assert.Fail($"Couldn't get the component from the specified prefab: {GAMEMANAGER_PATH}");
			}

			if (gameManager.QuickLoad)
			{
				Assert.Fail($"GameManager shouldn't have {nameof(gameManager.QuickLoad)} enabled!");
			}
		}
	}
}
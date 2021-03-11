using NUnit.Framework;
using UnityEngine;

namespace Tests.Config
{
	public class GameManagerTest
	{
		private const string GAMEMANAGER_PATH = "Prefabs/SceneConstruction/NestedManagers/GameManager";

		[Test]
		public void CheckQuickLoad()
		{
			var gameManagerPrefab = Resources.Load<GameObject>(GAMEMANAGER_PATH);
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
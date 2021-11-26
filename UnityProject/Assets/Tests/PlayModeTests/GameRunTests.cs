using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Editor;
using Messages.Server;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameRunTests
{
	public class GameRunTests
	{
		// A Test behaves as an ordinary method
		// [Test]
		// public void NewTestScriptSimplePasses()
		// {
		// 	Debug.Log("o3o A");
		// 	// Use the Assert class to test conditions
		// }

		// A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
		// `yield return null;` to skip a frame.
		[UnityTest]
		public IEnumerator NewTestScriptWithEnumeratorPasses()
		{
			// var gameManagerPrefabGUID = AssetDatabase.FindAssets("GameManager t:prefab", new string[] {"Assets/Prefabs/SceneConstruction/NestedManagers"});
			// var gameManagerPrefabPaths = gameManagerPrefabGUID.Select(AssetDatabase.GUIDToAssetPath).ToList();
			// var gameManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(gameManagerPrefabPaths.First());
			// if (gameManagerPrefab.TryGetComponent<GameManager>(out var gameManager))
			// {
			// 	gameManager.QuickLoad = true;
			//
			// }
			//
			// EditorUtility.SetDirty(gameManagerPrefab);
			// AssetDatabase.SaveAssets();


			yield return SceneManager.LoadSceneAsync("OnlineScene");

			for( int i = 0; i < SceneManager.sceneCount; i++ )
			{
				Logger.Log(	SceneManager.GetSceneAt(i).name);
			}

			var _GameManager = UnityEngine.Object.FindObjectOfType<GameManager>();

			_GameManager.QuickLoad = true;

			yield return TestSingleton.Instance.RunTests();

			// yield return WaitFor.Seconds(10);
			// RunRestartRound();
			// yield return WaitFor.Seconds(10);
			// RunRestartRound();
			// yield return WaitFor.Seconds(10);

			_GameManager.QuickLoad = false;
		}


		public static void RunRestartRound()
		{
			GameManager.Instance.RoundEndTime = 0f;
			GameManager.Instance.EndRound();
		}

		// public void RunRestartRound()
		// {
		// 	if (CustomNetworkManager.Instance._isServer == false)
		// 	{
		// 		Logger.Log("Can only execute command from server.", Category.DebugConsole);
		// 		return;
		// 	}
		//
		// 	Logger.Log("Triggered round restart from DebugConsole.", Category.DebugConsole);
		// 	GameManager.Instance.RoundEndTime = 1f;
		// 	GameManager.Instance.EndRound();
		// }
	}

}
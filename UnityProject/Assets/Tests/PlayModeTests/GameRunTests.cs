using System;
using System.Collections;
using System.Collections.Generic;
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
			int countLoaded = SceneManager.sceneCount;
			Scene[] loadedScenes = new Scene[countLoaded];
			for (int i = 0; i < countLoaded; i++)
			{
				yield return SceneManager.UnloadSceneAsync (SceneManager.GetSceneAt(i));
				break;
			}

			yield return SceneManager.LoadSceneAsync("OnlineScene");


			countLoaded = SceneManager.sceneCount;
			loadedScenes = new Scene[countLoaded];
			for (int i = 0; i < countLoaded; i++)
			{
				loadedScenes[i] = SceneManager.GetSceneAt(i);
			}
			foreach (var Scene in loadedScenes)
			{
				Logger.Log("Scene" + Scene.name);
				var roots = Scene.GetRootGameObjects();
				foreach (var root in roots)
				{
					{
						Logger.Log("root >" + root.name);
					}
				}
			}

			yield return WaitFor.Seconds(2);

			countLoaded = SceneManager.sceneCount;
			loadedScenes = new Scene[countLoaded];
			for (int i = 0; i < countLoaded; i++)
			{
				loadedScenes[i] = SceneManager.GetSceneAt(i);
			}
			foreach (var Scene in loadedScenes)
			{
				Logger.Log("Scene" + Scene.name);
				var roots = Scene.GetRootGameObjects();
				foreach (var root in roots)
				{
					{
						Logger.Log("root >" + root.name);
					}
				}
			}


			GameManager.Instance.QuickLoad = true;

			yield return TestSingleton.Instance.RunTests();

			// yield return WaitFor.Seconds(10);
			// RunRestartRound();
			// yield return WaitFor.Seconds(10);
			// RunRestartRound();
			// yield return WaitFor.Seconds(10);

			GameManager.Instance.QuickLoad = false;
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
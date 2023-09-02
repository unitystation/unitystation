using System.Collections;
using Logs;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameRunTests
{
	public class GameRunTests
	{
		[UnityTest]
		public IEnumerator NewTestScriptWithEnumeratorPasses()
		{
			yield return SceneManager.LoadSceneAsync("OnlineScene");

			if (GameManager.Instance == null)
			{
				Loggy.LogError("Unable to load OnlineScene Properly returning");
				yield break;
			}
			GameManager.Instance.QuickLoad = true;

			yield return TestSingleton.Instance.RunTests();

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

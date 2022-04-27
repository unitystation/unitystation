using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace GameRunTests
{
	public class GameRunTests
	{
		[UnityTestAttribute]
		public IEnumerator NewTestScriptWithEnumeratorPasses()
		{
			yield return new WaitForSeconds(10);
			yield return SceneManager.LoadSceneAsync("OnlineScene");
			yield return new WaitForSeconds(10);
			if (GameManager.Instance == null)
			{
				Logger.LogError("Unable to load OnlineScene Properly returning");
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
			PlayerManager.LocalPlayer = null;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
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
			Debug.Log("o3o B");
			// Use the Assert class to test conditions.
			// Use yield to skip a frame.

			Logger.Log("enter o3o" + DateTime.Now);

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Logger.Log(SceneManager.GetSceneAt(i).name);
			}

			Logger.Log("o3o A");
			yield return SceneManager.LoadSceneAsync("OnlineScene");
			//yield return SceneManager.LoadSceneAsync("RRT CleanStation");

			Logger.Log("end o3o" + DateTime.Now);

			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Logger.Log(SceneManager.GetSceneAt(i).name);
			}

			yield return WaitFor.Seconds(200);
			Logger.Log("end3 o3o" + DateTime.Now);
		}
	}
}
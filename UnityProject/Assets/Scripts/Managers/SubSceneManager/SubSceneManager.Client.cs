using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Client
public partial class SubSceneManager
{
	private bool clientIsLoadingSubscene = false;
	private List<SceneInfo> clientLoadedSubScenes = new List<SceneInfo>();

	private float waitTime = 0f;
	private readonly float tickRate = 1f;

	void MonitorServerSceneListOnClient()
	{
		if (isServer || clientIsLoadingSubscene) return;

		waitTime += Time.deltaTime;
		if (waitTime >= tickRate)
		{
			waitTime = 0f;
			if (clientLoadedSubScenes.Count < loadedScenesList.Count)
			{
				clientIsLoadingSubscene = true;
				var sceneToLoad = loadedScenesList[clientLoadedSubScenes.Count];
				clientLoadedSubScenes.Add(sceneToLoad);
				StartCoroutine(LoadClientSubScene(sceneToLoad));
			}
		}
	}

	IEnumerator LoadClientSubScene(SceneInfo sceneInfo)
	{
		if (sceneInfo.SceneType == SceneType.MainStation)
		{
			var clientLoadTimer = new SubsceneLoadTimer();
			//calculate load time:
			clientLoadTimer.MaxLoadTime = 10f;
			clientLoadTimer.IncrementLoadBar($"Loading {sceneInfo.SceneName}");
			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName, clientLoadTimer));
			MainStationLoaded = true;
			yield return WaitFor.Seconds(0.1f);
			UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		}
		else
		{
			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName));
		}

		clientIsLoadingSubscene = false;
	}
}

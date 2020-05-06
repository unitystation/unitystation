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
		var obsRequest = ObserverRequest.None;
		switch (sceneInfo.SceneType)
		{
			case SceneType.MainStation:
				obsRequest = ObserverRequest.RefreshForMainStation;
				break;
			case SceneType.AwaySite:
				obsRequest = ObserverRequest.RefreshForAwaySite;
				break;
		}

		yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName, obsRequest));
		clientIsLoadingSubscene = false;
	}
}

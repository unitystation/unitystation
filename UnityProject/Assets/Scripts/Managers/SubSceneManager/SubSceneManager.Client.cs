using System;
using System.Collections;
using System.Collections.Generic;
using Messages.Client;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

// Client
public partial class SubSceneManager
{
	private bool KillClientLoadingCoroutine = false;

	private bool clientIsLoadingSubscene = false;
	private List<SceneInfo> clientLoadedSubScenes = new List<SceneInfo>();

	private float waitTime = 0f;
	private readonly float tickRate = 1f;

	void MonitorServerSceneListOnClient()
	{
		if (isServer || clientIsLoadingSubscene || AddressableCatalogueManager.FinishLoaded == false) return;

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

	IEnumerator LoadClientSubScene(SceneInfo sceneInfo, bool HandlSynchronising = true,
		SubsceneLoadTimer SubsceneLoadTimer = null, bool OverrideclientIsLoadingSubscene = false)
	{
		if (sceneInfo.SceneType == SceneType.MainStation)
		{
			if (SubsceneLoadTimer == null)
			{
				SubsceneLoadTimer = new SubsceneLoadTimer();
				//calculate load time:
				SubsceneLoadTimer.MaxLoadTime = 10f;
			}

			SubsceneLoadTimer.IncrementLoadBar($"Loading {sceneInfo.SceneName}");
			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName, SubsceneLoadTimer, HandlSynchronising));
			MainStationLoaded = true;

		}
		else
		{
			if (SubsceneLoadTimer != null)
			{
				SubsceneLoadTimer.IncrementLoadBar(sceneInfo.SceneType != SceneType.HiddenScene ?
					$"Loading {sceneInfo.SceneName}" : "");
			}

			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName, HandlSynchronising  :HandlSynchronising ));
		}

		if (OverrideclientIsLoadingSubscene == false)
		{
			clientIsLoadingSubscene = false;
		}
	}

	public void LoadScenesFromServer(List<SceneInfo> Scenes, string OriginalScene, Action OnFinish)
	{
		KillClientLoadingCoroutine = false;
		StartCoroutine(LoadClientScenesFromServer(Scenes,OriginalScene, OnFinish));
	}

	private Action ClientSideFinishAction;

	IEnumerator LoadClientScenesFromServer(List<SceneInfo> Scenes, string OriginalScene, Action OnFinish)
	{
		ClientSideFinishAction = OnFinish;
		var SubsceneLoadTimer = new SubsceneLoadTimer();
		//calculate load time:
		SubsceneLoadTimer.MaxLoadTime = Scenes.Count;

		clientIsLoadingSubscene = true;
		foreach (var Scene in Scenes)
		{
			yield return LoadClientSubScene(Scene, false, SubsceneLoadTimer, true );
			if (KillClientLoadingCoroutine)
			{
				yield return SceneManager.UnloadSceneAsync(Scene.SceneName);
				KillClientLoadingCoroutine = false;
				clientIsLoadingSubscene = false;
				yield break;
			}
			clientLoadedSubScenes.Add(Scene);
		}

		NetworkClient.PrepareToSpawnSceneObjects();
		RequestObserverRefresh.Send(OriginalScene);

		foreach (var Scene in Scenes)
		{
			yield return WaitFor.Seconds(0.1f); //For smooth FPS not necessary technically, but causes freeze For a little bit
			if (KillClientLoadingCoroutine)
			{
				KillClientLoadingCoroutine = false;
				clientIsLoadingSubscene = false;
				yield break;
			}
			RequestObserverRefresh.Send(Scene.SceneName);
		}

		clientIsLoadingSubscene = false;
		yield return WaitFor.Seconds(0.1f); //For smooth FPS not necessary technically, but causes freeze For a little bit
		if (KillClientLoadingCoroutine)
		{
			KillClientLoadingCoroutine = false;
			yield break;
		}
		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		OnFinish.Invoke();
	}
}

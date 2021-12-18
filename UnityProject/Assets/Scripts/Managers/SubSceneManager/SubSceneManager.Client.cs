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

	IEnumerator LoadClientSubScene(SceneInfo sceneInfo, bool SynchronisingHandled = true, SubsceneLoadTimer SubsceneLoadTimer = null)
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
			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName, SubsceneLoadTimer));
			MainStationLoaded = true;

		}
		else
		{
			if (SubsceneLoadTimer != null)
			{
				SubsceneLoadTimer.IncrementLoadBar($"Loading {sceneInfo.SceneName}");
			}

			yield return StartCoroutine(LoadSubScene(sceneInfo.SceneName));
		}

		clientIsLoadingSubscene = false;
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
			clientIsLoadingSubscene = true;
			yield return LoadClientSubScene(Scene, false, SubsceneLoadTimer);
			if (KillClientLoadingCoroutine)
			{
				yield return SceneManager.UnloadSceneAsync(Scene.SceneName);
				KillClientLoadingCoroutine = false;
				clientIsLoadingSubscene = false;
				yield break;
			}
			clientIsLoadingSubscene = true;
			clientLoadedSubScenes.Add(Scene);
		}

		NetworkClient.PrepareToSpawnSceneObjects();
		yield return WaitFor.Seconds(0.2f);
		if (KillClientLoadingCoroutine)
		{
			KillClientLoadingCoroutine = false;
			clientIsLoadingSubscene = false;
			yield break;
		}

		RequestObserverRefresh.Send(OriginalScene);

		foreach (var Scene in Scenes)
		{
			yield return WaitFor.Seconds(0.1f);
			if (KillClientLoadingCoroutine)
			{
				KillClientLoadingCoroutine = false;
				clientIsLoadingSubscene = false;
				yield break;
			}
			RequestObserverRefresh.Send(Scene.SceneName);
		}

		clientIsLoadingSubscene = false;
		yield return WaitFor.Seconds(0.1f);
		if (KillClientLoadingCoroutine)
		{
			KillClientLoadingCoroutine = false;
			yield break;
		}
		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();
		OnFinish.Invoke();
	}
}

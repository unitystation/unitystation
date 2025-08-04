using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Messages.Client;
using Mirror;
using UI;
using UI.Systems.PreRound;
using UnityEngine;
using UnityEngine.SceneManagement;

// Client
public partial class SubSceneManager
{
	private bool KillClientLoadingCoroutine = false;

	public bool clientIsLoadingSubscene = false;
	public HashSet<SceneInfo> clientLoadedSubScenes = new HashSet<SceneInfo>();

	private float waitTime = 0f;
	private readonly float tickRate = 1f;

	public Dictionary<string, bool> ClientObserver = new Dictionary<string, bool>();

	public bool ClientIsFullyDoneLoadingOnSubsceneManager { get; private set; } = false;


	void MonitorServerSceneListOnClient()
	{
		if (CustomNetworkManager.IsServer || clientIsLoadingSubscene || AddressableCatalogueManager.FinishLoaded == false) return;

		waitTime += Time.deltaTime;
		if (waitTime < tickRate) return;
		waitTime = 0f;

		for (int i = 0; i < loadedScenesList.Count; i++)
		{
			GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Loading Scenes", $"Loading Scene Number #{i}", i / loadedScenesList.Count);
			var sceneToCheck = loadedScenesList[i];
			if(clientLoadedSubScenes.Contains(sceneToCheck)) continue;
			clientIsLoadingSubscene = true;
			StartCoroutine(LoadClientSubScene(sceneToCheck));
			break;
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
		clientLoadedSubScenes.Add(sceneInfo);
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

		var loadCount = 0;
		clientIsLoadingSubscene = true;
		foreach (var Scene in Scenes)
		{
			loadCount++;
			GUI_PreRoundWindow.Instance?.LoadingArea?.UpdateLoadingBar("Loading Scenes", $"Scenes Loaded.. ({loadCount}/{Scenes.Count})", loadCount / Scenes.Count);
			yield return LoadClientSubScene(Scene, false, SubsceneLoadTimer, true );
			if (KillClientLoadingCoroutine)
			{
				yield return SceneManager.UnloadSceneAsync(Scene.SceneName);
				KillClientLoadingCoroutine = false;
				clientIsLoadingSubscene = false;
				yield break;
			}
		}

		loadCount = 0;

		GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Loading Scenes", $"Preparing to spawn objects..", 0.4f);
		NetworkClient.PrepareToSpawnSceneObjects();
		RequestObserverRefresh.Send(OriginalScene);

		GUI_PreRoundWindow.Instance?.OnClientLoadUpdateStatus?.Invoke("Loading Scenes", $"Requesting Object Observers.. ({loadCount}/{Scenes.Count})", 0.5f);
		foreach (var scene in Scenes)
		{
			loadCount++;
			yield return WaitFor.Seconds(0.1f); //For smooth FPS not necessary technically, but causes freeze For a little bit
			if (KillClientLoadingCoroutine)
			{
				KillClientLoadingCoroutine = false;
				clientIsLoadingSubscene = false;
				yield break;
			}
			RequestObserverRefresh.Send(scene.SceneName);
			ClientObserver[scene.SceneName] = false;
		}

		clientIsLoadingSubscene = false;
		yield return WaitFor.Seconds(0.1f); //For smooth FPS not necessary technically, but causes freeze For a little bit
		if (KillClientLoadingCoroutine)
		{
			KillClientLoadingCoroutine = false;
			yield break;
		}

		int Count = 0;
		while (ObserverOfAll() == false && Count < 600)
		{
			yield return WaitFor.Seconds(0.1f);
			Count++;
		}


		ClientObserver.Clear();
		ClientSideFinishAction = null;
		OnFinish?.Invoke();
		GUI_PreRoundWindow.Instance?.HideLoadingArea();
		ClientIsFullyDoneLoadingOnSubsceneManager = true;
	}


	public bool ObserverOfAll()
	{
		return ClientObserver.All(x => x.Value);
	}

}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Messages.Client;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SubSceneManager : MonoBehaviour
{
	private static SubSceneManager subSceneManager;

	public static SubSceneManager Instance;

	public AwayWorldListSO awayWorldList;
	[SerializeField] private MainStationListSO mainStationList = null;
	[SerializeField] private AsteroidListSO asteroidList = null;
	[SerializeField] private AdditionalSceneListSO additionalSceneList = null;

	public ScenesSyncList loadedScenesList => SubSceneManagerNetworked.loadedScenesList;

	public MainStationListSO MainStationList => mainStationList;

	public bool AwaySiteLoaded { get; private set; }

	public bool IsMaintRooms
	{
		get { return serverChosenAwaySite == "Backrooms"; }
	}

	public bool MainStationLoaded { get; private set; }

	public bool SyndicateLoaded { get; private set; }
	public Scene SyndicateScene { get; private set; }
	public bool WizardLoaded { get; private set; }

	public SubSceneManagerNetworked SubSceneManagerNetworked;

	void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		EventManager.AddHandler(Event.RoundEnded, RoundEnded);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		EventManager.RemoveHandler(Event.RoundEnded, RoundEnded);
	}

	void RoundEnded() //So the client isn't loading scenes while server is Loading a new round
	{
		ClientSideFinishAction?.Invoke();
		KillClientLoadingCoroutine = true;
		ServerInitialLoadingComplete = false;
	}

	void UpdateMe()
	{
		MonitorServerSceneListOnClient();
	}

	/// <summary>
	/// General subscene loader
	/// </summary>
	/// <param name="sceneName"></param>
	/// <returns></returns>
	IEnumerator LoadSubScene(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true,
		SceneType sceneType = SceneType.HiddenScene)
	{

		if (CustomNetworkManager.IsServer == false)
		{
			if (clientLoadedSubScenes.Any(x => x.SceneName == sceneName))
			{
				Loggy.Log($"Scene already loaded client {sceneName}");
				yield break;
			}
		}

		ConnectionLoadedRecord[sceneName] = new HashSet<int>();
		AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		Loggy.Log($"AO Handle Generated for {sceneName}");
		if (AO != null)
		{
			Loggy.Log($"Waiting for AO.isDone {sceneName}");
			while (AO.isDone == false)
			{
				if (loadTimer != null) loadTimer.IncrementLoadBar();
				Loggy.Log($"Percentage loaded {sceneName} {AO.progress}");
				yield return null;
			}

			Loggy.Log($"Finished waiting for AO.isDone {sceneName}");
			if (loadTimer != null) loadTimer.IncrementLoadBar();
			if (CustomNetworkManager.IsServer)
			{
				Loggy.Log($"SpawnObjects + RequestObserverRefresh {sceneName}");
				NetworkServer.SpawnObjects();

				while (NetworkClient.connection.isAuthenticated == false) //Needed so that if Authentication takes time, server instance does not disconnect itself.
				{
					yield return null;
				}
				RequestObserverRefresh.Send(sceneName);
			}
			else
			{
				if (HandlSynchronising)
				{
					NetworkClient.PrepareToSpawnSceneObjects();
					yield return WaitFor.Seconds(0.2f);
					RequestObserverRefresh.Send(sceneName);
				}
			}

			if (CustomNetworkManager.IsServer)
			{
				Loggy.Log($"SloadedScenesList.add {sceneName}");
				loadedScenesList.Add(new SceneInfo
				{
					SceneName = sceneName,
					SceneType = sceneType
				});
				SubSceneManagerNetworked.netIdentity.isDirty = true;
			}
		}
		else
		{
			Loggy.LogError($"was unable to find scene for {sceneName} Skipping");
		}
		Loggy.Log($"Finished loading {sceneName}");
	}

	public static void ProcessObserverRefreshReq(PlayerInfo connectedPlayer, Scene sceneContext)
	{
		if (connectedPlayer.Connection != null)
		{
			if (ConnectionLoadedRecord.ContainsKey(sceneContext.name) == false)
			{
				ConnectionLoadedRecord[sceneContext.name] = new HashSet<int>();
			}

			ConnectionLoadedRecord[sceneContext.name].Add(connectedPlayer.Connection.connectionId);

			Instance.AddObserverToAllObjects(connectedPlayer.Connection, sceneContext);
		}
	}
}

public enum SceneType
{
	MainStation,
	AwaySite,
	Asteroid,
	AdditionalScenes,
	Space,
	HiddenScene
}

[System.Serializable]
public struct SceneInfo : IEquatable<SceneInfo>
{
	public string SceneName;
	public SceneType SceneType;

	public bool Equals(SceneInfo other)
	{
		return SceneName == other.SceneName && SceneType == other.SceneType;
	}

	public override bool Equals(object obj)
	{
		return obj is SceneInfo other && Equals(other);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return ((SceneName != null ? SceneName.GetHashCode() : 0) * 397) ^ (int) SceneType;
		}
	}
}

[System.Serializable]
public class ScenesSyncList : SyncList<SceneInfo>
{
}

public class SubsceneLoadTimer
{
	public float MaxLoadTime;
	public float CurrentLoadTime;

	private string lastText;

	public void IncrementLoadBar(string text = "")
	{
		var textToDisplay = "";
		if (string.IsNullOrEmpty(text))
		{
			textToDisplay = lastText;
		}
		else
		{
			textToDisplay = text;
		}

		lastText = textToDisplay;

		CurrentLoadTime += 1f;
		UIManager.Display.preRoundWindow.UpdateLoadingBar(textToDisplay, CurrentLoadTime / MaxLoadTime);
	}
}
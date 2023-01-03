using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Managers.SubSceneManager;
using Messages.Client;
using Mirror;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
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

	public AssetReference MaintRoomsRef;

	public bool IsMaintRooms => serverChosenAwaySite == MaintRoomsRef;

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
		InitialLoadingComplete = false;
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
	private IEnumerator LoadSubScene(AssetReference sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true)
	{
		if (sceneName == null)
		{
			Logger.LogError("[SubSceneManager] - Attempted to pass null asset reference while loading.. Skipping.");
			yield break;
		}

		AsyncOperationHandle<SceneInstance> AO = new AsyncOperationHandle<SceneInstance>();

		try
		{
			AO = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive, false);
		}
		catch (Exception e)
		{
			Logger.LogError($"[SubSceneManager] - Something went wrong while trying to load a scene... \n {e}");
			yield break;
		}

		while (AO.IsDone == false)
		{
			loadTimer?.IncrementLoadBar();
			yield return WaitFor.EndOfFrame;
		}

		yield return AO.Result.ActivateAsync();

		loadTimer?.IncrementLoadBar();
		if (isServer)
		{
			NetworkServer.SpawnObjects();
			RequestObserverRefresh.Send(AO.Result.Scene.name);
		}
		else
		{
			if (HandlSynchronising == false) yield break;
			NetworkClient.PrepareToSpawnSceneObjects();
			yield return WaitFor.Seconds(0.2f);
			RequestObserverRefresh.Send(AO.Result.Scene.name);
		}

		sceneNames.Add(sceneName, AO.Result.Scene.name);
	}

	IEnumerator LoadSubSceneFromString(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true)
	{
		if (sceneName == null)
		{
			Logger.LogError("[SubSceneManager] - Attempted to pass null asset reference while loading.. Skipping.");
			yield break;
		}
		ConnectionLoadedRecord[sceneName] = new HashSet<int>();

		var AO = new AsyncOperationHandle<SceneInstance>();
		if (CustomNetworkManager.IsServer == false)
		{
			if(clientLoadedSubScenes.Any(x=> x.SceneName == sceneName)) yield break;
		}
		try
		{
			AO = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		}
		catch (Exception e)
		{
			Logger.LogError($"[SubSceneManager] - Something went wrong while trying to load a scene... \n {e}");
			yield break;
		}
		while (AO.IsDone == false)
		{
			loadTimer?.IncrementLoadBar();
			yield return WaitFor.EndOfFrame;
		}

		loadTimer?.IncrementLoadBar();
		if (isServer)
		{
			NetworkServer.SpawnObjects();
			RequestObserverRefresh.Send(sceneName);
		}
		else
		{
			if (HandlSynchronising == false) yield break;
			NetworkClient.PrepareToSpawnSceneObjects();
			yield return WaitFor.Seconds(0.2f);
			RequestObserverRefresh.Send(sceneName);
		}
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
	public string SceneKey;
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
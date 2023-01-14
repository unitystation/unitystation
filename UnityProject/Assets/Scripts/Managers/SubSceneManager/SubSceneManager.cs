using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Managers.SubSceneManager;
using Messages.Client;
using Mirror;
using Newtonsoft.Json;
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
	private MapsConfig mainStationListJson;
	private List<MainStationInfo> allmainstationmaps = new List<MainStationInfo>();
	[SerializeField] private AsteroidListSO asteroidList = null;
	[SerializeField] private AdditionalSceneListSO additionalSceneList = null;

	public ScenesSyncList loadedScenesList => SubSceneManagerNetworked.loadedScenesList;

	public List<MainStationInfo> MainStationList => allmainstationmaps;

	public bool AwaySiteLoaded { get; private set; }

	public string MaintRoomsRef;

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
		SetupMainstationData();
	}

	private void SetupMainstationData()
	{
		void CreateInfo(string sceneName, string sceneKey)
		{
			var info = new MainStationInfo
			{
				Name = sceneName,
				Key = sceneKey
			};
			allmainstationmaps.Add(info);
		}
		var mapData = File.ReadAllText($"{Application.streamingAssetsPath}/maps.json");
		mainStationListJson = JsonConvert.DeserializeObject<MapsConfig>(mapData);
		foreach (var map in mainStationListJson.lowPopMaps)
		{
			CreateInfo(map[0], map[1]);
		}
		foreach (var map in mainStationListJson.medPopMaps)
		{
			CreateInfo(map[0], map[1]);
		}
		foreach (var map in mainStationListJson.highPopMaps)
		{
			CreateInfo(map[0], map[1]);
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

	private IEnumerator HandleObjectsSync(bool handleSynchronising, AsyncOperationHandle<SceneInstance> AO)
	{
		if (isServer)
		{
			NetworkServer.SpawnObjects();
			RequestObserverRefresh.Send(AO.Result.Scene.name);
		}
		else
		{
			if (handleSynchronising == false) yield break;
			try
			{
				NetworkClient.PrepareToSpawnSceneObjects();
			}
			catch (Exception e)
			{
				Logger.LogError($"[SubSceneManager/LoadSubScene/PrepareToSpawnSceneObjects] - SCENE NOT SYNCED PROPERLY!! \n {e}");
			}
			yield return WaitFor.Seconds(0.2f);
			RequestObserverRefresh.Send(AO.Result.Scene.name);
		}
	}

	/// <summary>
	/// General subscene loader
	/// </summary>
	/// <param name="sceneName"></param>
	/// <returns></returns>
	private IEnumerator LoadSubScene(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true, SceneType type = SceneType.MainStation)
	{
		if (string.IsNullOrEmpty(sceneName))
		{
			Logger.LogError("[SubSceneManager] - Attempted to pass null or empty asset reference while loading.. Skipping.");
			yield break;
		}

		var AO = new AsyncOperationHandle<SceneInstance>();

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
		yield return StartCoroutine(HandleObjectsSync(HandlSynchronising, AO));

		if (CustomNetworkManager.IsServer == false) yield break;
		//Only update this on the server, otherwise clients will break.
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = AO.Result.Scene.name,
			SceneKey = sceneName,
			SceneType = type
		});
	}

	private IEnumerator LoadSubScene(AssetReference sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true, SceneType type = SceneType.Space)
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
		yield return StartCoroutine(HandleObjectsSync(HandlSynchronising, AO));

		if (CustomNetworkManager.IsServer == false) yield break;
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = AO.Result.Scene.name,
			SceneKey = sceneName.AssetGUID,
			SceneType = type
		});
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

public struct MapsConfig
{
	public List<List<string>> lowPopMaps { get; set; }
	public List<List<string>> medPopMaps { get; set; }
	public List<List<string>> highPopMaps { get; set; }
	public int medPopMinLimit { get; set; }
	public int highPopMinLimit { get; set; }
}

public struct MainStationInfo
{
	public string Name;
	public string Key;
}
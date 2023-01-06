using System;
using System.Collections;
using System.Collections.Generic;
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

public partial class SubSceneManager : NetworkBehaviour
{
	private static SubSceneManager subSceneManager;

	public static SubSceneManager Instance;

	public AwayWorldListSO awayWorldList;
	private MapsConfig mainStationListJson;
	private List<MainStationInfo> allmainstationmaps = new List<MainStationInfo>();
	[SerializeField] private AsteroidListSO asteroidList = null;
	[SerializeField] private AdditionalSceneListSO additionalSceneList = null;

	public readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public List<MainStationInfo> MainStationList => allmainstationmaps;

	public bool AwaySiteLoaded { get; private set; }

	public string MaintRoomsRef;

	public bool IsMaintRooms => serverChosenAwaySite == MaintRoomsRef;

	public bool MainStationLoaded { get; private set; }

	public bool SyndicateLoaded { get; private set; }
	public Scene SyndicateScene { get; private set; }
	public bool WizardLoaded { get; private set; }

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
		var mapData = File.ReadAllText($"{Application.streamingAssetsPath}/maps.json");
		mainStationListJson = JsonConvert.DeserializeObject<MapsConfig>(mapData);
		foreach (var map in mainStationListJson.lowPopMaps)
		{
			var info = new MainStationInfo
			{
				Name = map[0],
				Key = map[1]
			};
			Logger.Log($"{info.Name} -> {info.Key}");
			allmainstationmaps.Add(info);
		}
		foreach (var map in mainStationListJson.medPopMaps)
		{
			var info = new MainStationInfo
			{
				Name = map[0],
				Key = map[1]
			};
			Logger.Log($"{info.Name} -> {info.Key}");
			allmainstationmaps.Add(info);
		}
		foreach (var map in mainStationListJson.highPopMaps)
		{
			var info = new MainStationInfo
			{
				Name = map[0],
				Key = map[1]
			};
			Logger.Log($"{info.Name} -> {info.Key}");
			allmainstationmaps.Add(info);
		}
	}

	private void OnEnable()
	{
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
		EventManager.AddHandler(Event.RoundEnded, KillClientCoroutine);
	}

	private void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
		EventManager.RemoveHandler(Event.RoundEnded, KillClientCoroutine);
	}

	void KillClientCoroutine() //So the client isn't loading scenes while server is Loading a new round
	{
		ClientSideFinishAction?.Invoke();
		KillClientLoadingCoroutine = true;
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
	private IEnumerator LoadSubScene(string sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true)
	{
		if (sceneName == null)
		{
			Logger.LogError("[SubSceneManager] - Attempted to pass null asset reference while loading.. Skipping.");
			yield break;
		}

		var AO = new AsyncOperationHandle<SceneInstance>();

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

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = sceneName,
			SceneKey = SpaceSceneRef.AssetGUID,
			SceneType = SceneType.Space
		});
	}

	private IEnumerator LoadSubScene(AssetReference sceneName, SubsceneLoadTimer loadTimer = null, bool HandlSynchronising = true)
	{
		if (sceneName == null)
		{
			Logger.LogError("[SubSceneManager] - Attempted to pass null asset reference while loading.. Skipping.");
			yield break;
		}

		var AO = new AsyncOperationHandle<SceneInstance>();

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

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = AO.Result.Scene.name,
			SceneKey = SpaceSceneRef.AssetGUID,
			SceneType = SceneType.Space
		});
	}

	public static void ProcessObserverRefreshReq(PlayerInfo connectedPlayer, Scene sceneContext)
	{
		if (connectedPlayer.Connection != null)
		{
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
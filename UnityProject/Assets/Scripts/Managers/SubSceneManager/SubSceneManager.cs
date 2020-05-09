using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class SubSceneManager : NetworkBehaviour
{
	private static SubSceneManager subSceneManager;

	public static SubSceneManager Instance
	{
		get
		{
			if (subSceneManager == null)
			{
				subSceneManager = FindObjectOfType<SubSceneManager>();
			}

			return subSceneManager;
		}
	}

	public AwayWorldListSO awayWorldList;
	[SerializeField] private MainStationListSO mainStationList = null;
	[SerializeField] private AsteroidListSO asteroidList = null;

	readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	public bool AwaySiteLoaded { get; private set; }
	public bool MainStationLoaded { get; private set; }

	void Update()
	{
		MonitorServerSceneListOnClient();
	}

	/// <summary>
	/// General subscene loader
	/// </summary>
	/// <param name="sceneName"></param>
	/// <returns></returns>
	IEnumerator LoadSubScene(string sceneName, SubsceneLoadTimer loadTimer = null)
	{
		AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		while (!AO.isDone)
		{
			if (loadTimer != null) loadTimer.IncrementLoadBar();
			yield return WaitFor.EndOfFrame;
		}

		if (loadTimer != null) loadTimer.IncrementLoadBar();
		yield return WaitFor.EndOfFrame;

		if (loadTimer != null) loadTimer.IncrementLoadBar();
		if (isServer)
		{
			NetworkServer.SpawnObjects();
		}
		else
		{
			ClientScene.PrepareToSpawnSceneObjects();
			yield return WaitFor.EndOfFrame;
			RequestObserverRefresh.Send(sceneName);
		}
	}

	public static void ProcessObserverRefreshReq(ConnectedPlayer connectedPlayer, Scene sceneContext)
	{
		if (connectedPlayer.Connection != null)
		{
			Instance.AddObserverToAllObjects(connectedPlayer.Connection, sceneContext);
		}
	}

	public void WaitForSubScene(string data, uint id)
	{
		StartCoroutine(WaitForMapToLoad(data, id));
	}
	IEnumerator WaitForMapToLoad(string data, uint managerId)
	{
		while (!NetworkIdentity.spawned.ContainsKey(managerId))
		{
			yield return WaitFor.EndOfFrame;
		}

		TileChangeManager tm = NetworkIdentity.spawned[managerId].GetComponent<TileChangeManager>();
		tm.InitServerSync(data);
	}
}

public enum SceneType
{
	MainStation,
	AwaySite,
	Asteroid
}

[System.Serializable]
public struct SceneInfo
{
	public string SceneName;
	public SceneType SceneType;
}

[System.Serializable]
public class ScenesSyncList : SyncList<SceneInfo>{}

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
using System;
using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SubSceneManager : NetworkBehaviour
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

	[SerializeField] private AwayWorldListSO awayWorldList;
	[SerializeField] private MainStationListSO mainStationList;

	readonly ScenesSyncList loadedScenesList = new ScenesSyncList();

	private string serverChosenAwaySite;
	public string ChosenAwaySite
	{
		get => serverChosenAwaySite;
	}

	private string serverChosenMainStation;
	public string ChosenMainStation
	{
		get => serverChosenMainStation;
	}

	public bool AwaySiteLoaded { get; private set; }
	public bool MainStationLoaded { get; private set; }

	[Tooltip("Plan the best time to start loading the away world on the " +
	         "server/host. This should be done slighty after main scene load and before " +
	         "round start.")]

	private float serverWaitAwayWorldLoad = 30f;

	public override void OnStartServer()
	{
		// Determine a Main station subscene and away site
		StartCoroutine(RoundStartServerLoadSequence());
		base.OnStartServer();
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
	}

	private void OnEnable()
	{
		loadedScenesList.Callback += OnLoadedScenesList;
	}

	private void OnDisable()
	{
		loadedScenesList.Callback -= OnLoadedScenesList;
	}

	private void OnLoadedScenesList(SyncList<SceneInfo>.Operation op, int itemindex, SceneInfo olditem,
		SceneInfo newitem)
	{
		//I have no idea how to make this hook trigger on clients so its Rpc time:
		RpcSceneLoaded(itemindex, newitem);
	}

	[ClientRpc]
	void RpcSceneLoaded(int itemIndex, SceneInfo sceneInfo)
	{
		//Server loaded in a new subscene
	}


	IEnumerator RoundStartServerLoadSequence()
	{
		yield return WaitFor.Seconds(0.1f);
		MainStationLoaded = true;
		serverChosenMainStation = mainStationList.MainStations[0];
		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation));
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = serverChosenMainStation,
			SceneType = SceneType.MainStation
		});

		yield return WaitFor.Seconds(serverWaitAwayWorldLoad);
		//Load the away site
		AwaySiteLoaded = true;
		serverChosenAwaySite = awayWorldList.GetRandomAwaySite();
	//	yield return StartCoroutine(LoadSubScene(serverChosenAwaySite));

		loadedScenesList.Add(new SceneInfo
		{
			SceneName = serverChosenAwaySite,
			SceneType = SceneType.AwaySite
		});

		Logger.Log($"Server has loaded {serverChosenAwaySite} away site", Category.SubScenes);
	}

	/// <summary>
	/// General subscene loader
	/// </summary>
	/// <param name="sceneName"></param>
	/// <returns></returns>
	IEnumerator LoadSubScene(string sceneName, ObserverRequest obsReq = ObserverRequest.None)
	{
		if (obsReq == ObserverRequest.RefreshForAwaySite) yield return WaitFor.Seconds(6f);
		AsyncOperation AO = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		while (!AO.isDone)
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
		if (isServer)
		{
			NetworkServer.SpawnObjects();
		}
		else
		{
			if (obsReq != ObserverRequest.None)
			{
				yield return WaitFor.Seconds(1f);
				ClientScene.PrepareToSpawnSceneObjects();
				RequestObserverRefresh.Send(obsReq);
			}
		}
	}

	public static void ProcessObserverRefreshReq(ConnectedPlayer connectedPlayer, ObserverRequest requestType)
	{
		if (connectedPlayer.Connection != null)
		{
			Instance.AddObserverToAllObjects(connectedPlayer.Connection, requestType);
		}
	}

	/// <summary>
	/// No scene / proximity visibility checking. Just adding it to everything
	/// </summary>
	/// <param name="connToAdd"></param>
	void AddObserverToAllObjects(NetworkConnection connToAdd, ObserverRequest request)
	{
		foreach (var n in NetworkIdentity.spawned)
		{
			if (n.Value.gameObject.scene == SceneManager.GetActiveScene())
			{
				n.Value.AddPlayerObserver(connToAdd);
				continue;
			}

			switch (request)
			{
				case ObserverRequest.RefreshForMainStation:
					if (n.Value.gameObject.scene.name == ChosenMainStation)
					{
						n.Value.AddPlayerObserver(connToAdd);
					}
					break;
				case ObserverRequest.RefreshForAwaySite:
					if (n.Value.gameObject.scene.name == ChosenAwaySite)
					{
						n.Value.AddPlayerObserver(connToAdd);
					}
					break;
			}
		}

		connToAdd.Send(new ObjectSpawnFinishedMessage());
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
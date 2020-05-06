using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

//Server
public partial class SubSceneManager
{
	private string serverChosenAwaySite;
	private string serverChosenMainStation;

	private float serverWaitAwayWorldLoad = 30f;

	public override void OnStartServer()
	{
		// Determine a Main station subscene and away site
		StartCoroutine(RoundStartServerLoadSequence());
		base.OnStartServer();
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
					if (n.Value.gameObject.scene.name == Instance.serverChosenMainStation)
					{
						n.Value.AddPlayerObserver(connToAdd);
					}
					break;
				case ObserverRequest.RefreshForAwaySite:
					if (n.Value.gameObject.scene.name == Instance.serverChosenAwaySite)
					{
						n.Value.AddPlayerObserver(connToAdd);
					}
					break;
			}
		}

		connToAdd.Send(new ObjectSpawnFinishedMessage());
	}
}

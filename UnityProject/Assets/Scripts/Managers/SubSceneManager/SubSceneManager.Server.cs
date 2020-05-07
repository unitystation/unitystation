using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

//Server
public partial class SubSceneManager
{
	private string serverChosenAwaySite;
	private string serverChosenMainStation;

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

		//Auto scene load stuff in editor:
		var prevEditorScene = "";

#if UNITY_EDITOR
		if (EditorPrefs.HasKey("prevEditorScene"))
		{
			if (!string.IsNullOrEmpty(EditorPrefs.GetString("prevEditorScene")))
			{
				prevEditorScene = EditorPrefs.GetString("prevEditorScene");
			}
		}
#endif

		if (mainStationList.MainStations.Contains(prevEditorScene))
		{
			serverChosenMainStation = prevEditorScene;
		}
		else
		{
			serverChosenMainStation = mainStationList.GetRandomMainStation();
		}

		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation));
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = serverChosenMainStation,
			SceneType = SceneType.MainStation
		});

		yield return WaitFor.Seconds(0.1f);

		foreach (var asteroid in asteroidList.Asteroids)
		{
			yield return StartCoroutine(LoadSubScene(asteroid));

			loadedScenesList.Add(new SceneInfo
			{
				SceneName = asteroid,
				SceneType = SceneType.Asteroid
			});

			yield return WaitFor.Seconds(0.1f);
		}

		//Load the away site
		if (awayWorldList.AwayWorlds.Contains(prevEditorScene))
		{
			serverChosenAwaySite = prevEditorScene;
		}
		else
		{
			serverChosenAwaySite = awayWorldList.GetRandomAwaySite();
		}

		yield return StartCoroutine(LoadSubScene(serverChosenAwaySite));
		AwaySiteLoaded = true;
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
	void AddObserverToAllObjects(NetworkConnection connToAdd, Scene sceneContext)
	{
		foreach (var n in NetworkIdentity.spawned)
		{
			if (n.Value.gameObject.scene == SceneManager.GetActiveScene())
			{
				n.Value.AddPlayerObserver(connToAdd);
				continue;
			}

			if (n.Value.gameObject.scene == sceneContext)
			{
				n.Value.AddPlayerObserver(connToAdd);
			}
		}

		connToAdd.Send(new ObjectSpawnFinishedMessage());
	}
}

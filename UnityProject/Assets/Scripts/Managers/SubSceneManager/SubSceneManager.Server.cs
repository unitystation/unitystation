using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine.SceneManagement;

//Server
public partial class SubSceneManager
{
	private string serverChosenAwaySite = "loading";
	private string serverChosenMainStation = "loading";

	public static string ServerChosenMainStation
	{
		get { return Instance.serverChosenMainStation; }
	}

	public override void OnStartServer()
	{
		NetworkServer.observerSceneList.Clear();
		// Determine a Main station subscene and away site
		StartCoroutine(RoundStartServerLoadSequence());
		base.OnStartServer();
	}

	/// <summary>
	/// Starts a collection of scenes that this connection is allowed to see
	/// </summary>
	public void AddNewObserverScenePermissions(NetworkConnection conn)
	{
		if (NetworkServer.observerSceneList.ContainsKey(conn))
		{
			NetworkServer.observerSceneList.Remove(conn);
		}

		NetworkServer.observerSceneList.Add(conn, new List<Scene> {SceneManager.GetActiveScene()});
	}

	IEnumerator RoundStartServerLoadSequence()
	{
		var loadTimer = new SubsceneLoadTimer();
		//calculate load time:
		loadTimer.MaxLoadTime = 20f + (asteroidList.Asteroids.Count * 10f);
		loadTimer.IncrementLoadBar("Preparing..");
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

		loadTimer.IncrementLoadBar($"Loading {serverChosenMainStation}");
		//load main station
		yield return StartCoroutine(LoadSubScene(serverChosenMainStation, loadTimer));
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = serverChosenMainStation,
			SceneType = SceneType.MainStation
		});

		yield return WaitFor.Seconds(0.1f);

		loadTimer.IncrementLoadBar("Loading Asteroids");

		foreach (var asteroid in asteroidList.Asteroids)
		{
			yield return StartCoroutine(LoadSubScene(asteroid, loadTimer));

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
		loadTimer.IncrementLoadBar("Loading Away Site");
		yield return StartCoroutine(LoadSubScene(serverChosenAwaySite, loadTimer));
		AwaySiteLoaded = true;
		loadedScenesList.Add(new SceneInfo
		{
			SceneName = serverChosenAwaySite,
			SceneType = SceneType.AwaySite
		});

		netIdentity.isDirty = true;

		yield return WaitFor.Seconds(0.1f);
		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();

		Logger.Log($"Server has loaded {serverChosenAwaySite} away site", Category.SubScenes);
	}

	/// <summary>
	/// Add a new scene to a specific connections observable list
	/// </summary>
	void AddObservableSceneToConnection(NetworkConnection conn, Scene sceneContext)
	{
		if (!NetworkServer.observerSceneList.ContainsKey(conn))
		{
			AddNewObserverScenePermissions(conn);
		}

		if (!NetworkServer.observerSceneList[conn].Contains(sceneContext))
		{
			NetworkServer.observerSceneList[conn].Add(sceneContext);
		}
	}

	/// <summary>
	/// No scene / proximity visibility checking. Just adding it to everything
	/// </summary>
	/// <param name="connToAdd"></param>
	void AddObserverToAllObjects(NetworkConnection connToAdd, Scene sceneContext)
	{
		AddObservableSceneToConnection(connToAdd, sceneContext);
		StartCoroutine(SyncPlayerData(connToAdd, sceneContext));
	}

	/// Sync init data with specific scenes
	/// staggered over multiple frames
	public IEnumerator SyncPlayerData(NetworkConnection connToAdd, Scene sceneContext)
	{
		Logger.LogFormat("SyncPlayerData. This server sending a bunch of sync data to new " +
		                 "client {0} for scene {1}", Category.Connections, connToAdd.clientOwnedObjects.ElementAt(0).gameObject, sceneContext.name);

		//Activate the matrices on the client first
		foreach (var m in MatrixManager.Instance.ActiveMatrices)
		{
			if (m.Matrix.gameObject.scene == sceneContext)
			{
				m.Matrix.GetComponentInParent<NetworkIdentity>().AddPlayerObserver(connToAdd);
				yield return WaitFor.EndOfFrame;
			}
		}

		yield return WaitFor.EndOfFrame;

		//Now start all of the networked objects on those matrices we
		//activated earlier. We are doing it here in the coroutine
		//so we can stagger the awake updates on the client
		//This should avoid massive spike in Physics2D when the colliders
		//come active. We do this in lots of 20 every frame
		int objCount = 0;
		var netIds = NetworkIdentity.spawned.Values.ToList();
		foreach (var n in netIds)
		{
			if (n.gameObject.scene == sceneContext)
			{
				n.AddPlayerObserver(connToAdd);
				objCount += 1;
				if (objCount >= 20)
				{
					objCount = 0;
					yield return WaitFor.EndOfFrame;
				}
			}
		}

		//TileChange Data
		TileChangeManager[] tcManagers = FindObjectsOfType<TileChangeManager>();
		for (var i = 0; i < tcManagers.Length; i++)
		{
			if(tcManagers[i].gameObject.scene != sceneContext) continue;
			tcManagers[i].NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject);
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;

		//All matrices
		MatrixMove[] matrices = FindObjectsOfType<MatrixMove>();
		for (var i = 0; i < matrices.Length; i++)
		{
			if(matrices[i].gameObject.scene != sceneContext) continue;
			matrices[i].NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject, true);
		}

		yield return WaitFor.EndOfFrame;

		//All transforms
		objCount = 0;
		CustomNetTransform[] scripts = FindObjectsOfType<CustomNetTransform>();
		for (var i = 0; i < scripts.Length; i++)
		{
			if(scripts[i].gameObject.scene != sceneContext) continue;
			scripts[i].NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject);
			//Again we are trying to limit physics 2d spikes on the client
			//20 notifys a frame:
			objCount += 1;
			if (objCount >= 20)
			{
				objCount = 0;
				yield return WaitFor.EndOfFrame;
			}
		}

		yield return WaitFor.EndOfFrame;

		//All player bodies
		PlayerSync[] playerBodies = FindObjectsOfType<PlayerSync>();
		for (var i = 0; i < playerBodies.Length; i++)
		{
			if(playerBodies[i].gameObject.scene != sceneContext) continue;
			var playerBody = playerBodies[i];
			playerBody.NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject, true);

			var playerSprites = playerBody.GetComponent<PlayerSprites>();
			if (playerSprites)
			{
				playerSprites.NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject);
			}
			var equipment = playerBody.GetComponent<Equipment>();
			if (equipment)
			{
				equipment.NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject);
			}
		}

		yield return WaitFor.EndOfFrame;

		//Doors
		DoorController[] doors = FindObjectsOfType<DoorController>();
		for (var i = 0; i < doors.Length; i++)
		{
			if(doors[i].gameObject.scene != sceneContext) continue;
			doors[i].NotifyPlayer(connToAdd.clientOwnedObjects.ElementAt(0).gameObject);
		}
		Logger.Log($"Sent sync data ({matrices.Length} matrices, {scripts.Length} transforms, {playerBodies.Length} players) " +
		           $"to {connToAdd.clientOwnedObjects.ElementAt(0).gameObject.name}", Category.Connections);
	}
}

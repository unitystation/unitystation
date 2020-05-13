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

		//Choose and load a mainstation
		yield return StartCoroutine(ServerLoadMainStation(loadTimer));
		//Load Asteroids:
		yield return StartCoroutine(ServerLoadAsteroids(loadTimer));
		//Load away site:
		yield return StartCoroutine(ServerLoadAwaySite(loadTimer));

		netIdentity.isDirty = true;

		yield return WaitFor.Seconds(0.1f);
		UIManager.Display.preRoundWindow.CloseMapLoadingPanel();

		Logger.Log($"Server has loaded {serverChosenAwaySite} away site", Category.SubScenes);
	}

	//Choose and load a main station on the server
	IEnumerator ServerLoadMainStation(SubsceneLoadTimer loadTimer)
	{
		MainStationLoaded = true;
		//Auto scene load stuff in editor:
		var prevEditorScene = GetEditorPrevScene();
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
	}

	//Load all the asteroids on the server
	IEnumerator ServerLoadAsteroids(SubsceneLoadTimer loadTimer)
	{
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
	}

	//Load the away site on the server
	IEnumerator ServerLoadAwaySite(SubsceneLoadTimer loadTimer)
	{
		var prevEditorScene = GetEditorPrevScene();
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
	}

	string GetEditorPrevScene()
	{
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
		return prevEditorScene;
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
		StartCoroutine(SyncPlayerData(connToAdd, sceneContext));
		AddObservableSceneToConnection(connToAdd, sceneContext);
	}

	/// Sync init data with specific scenes
	/// staggered over multiple frames
	public IEnumerator SyncPlayerData(NetworkConnection connToAdd, Scene sceneContext)
	{
		Logger.LogFormat("SyncPlayerData. This server sending a bunch of sync data to new " +
		                 "client {0} for scene {1}", Category.Connections, connToAdd.clientOwnedObjects.ElementAt(0).gameObject, sceneContext.name);

		//Add connection as observer to the scene objects:
		yield return StartCoroutine(AddObserversForClient(connToAdd, sceneContext));
		//All matrices
		yield return StartCoroutine(InitializeMatricesForClient(connToAdd, sceneContext));
		//Sync Tilemaps
		yield return StartCoroutine(InitializeTileMapsForClient(connToAdd, sceneContext));
		//All transforms
		yield return StartCoroutine(InitializeTransformsForClient(connToAdd, sceneContext));
		//All Players
		yield return StartCoroutine(InitializePlayersForClient(connToAdd, sceneContext));
		//All Doors
		yield return StartCoroutine(InitializeDoorsForClient(connToAdd, sceneContext));
	}

	/// <summary>
	/// Add a connection as an observer to everything in a scene
	/// </summary>
	IEnumerator AddObserversForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//Activate the matrices on the client first
		for (int i = MatrixManager.Instance.ActiveMatrices.Count - 1; i >= 0; i--)
		{
			var matrix = MatrixManager.Instance.ActiveMatrices[i].Matrix;
			if (matrix.gameObject.scene == sceneContext)
			{
				matrix.GetComponentInParent<NetworkIdentity>().AddPlayerObserver(connToAdd);
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
			if (n == null || n.gameObject == null
			              || n.gameObject.scene != sceneContext) continue;

			n.AddPlayerObserver(connToAdd);
			objCount += 1;
			if (objCount >= 20)
			{
				objCount = 0;
				yield return WaitFor.EndOfFrame;
			}
		}
	}

	//Force a notify update for all tilemaps in a given scene to a client connection
	IEnumerator InitializeTileMapsForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//TileChange Data
		TileChangeManager[] tcManagers = FindObjectsOfType<TileChangeManager>();
		for (var i = 0; i < tcManagers.Length; i++)
		{
			if(tcManagers[i] == null ||
			   tcManagers[i].gameObject.scene != sceneContext) continue;

			Debug.Log("Try to update tile maps to client " + tcManagers[i].gameObject.name);
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null)
			{
				tcManagers[i].NotifyPlayer(playerOwnedObj);
			}
			else
			{
				Debug.Log("TILEMAP CANT FIND A PLAYER OBJ TO SEND TOO!!");
			}
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Matrices in a given scene to a client connection
	IEnumerator InitializeMatricesForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		MatrixMove[] matrices = FindObjectsOfType<MatrixMove>();
		for (var i = 0; i < matrices.Length; i++)
		{
			if(matrices[i].gameObject.scene != sceneContext) continue;
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if(playerOwnedObj != null) matrices[i].NotifyPlayer(playerOwnedObj, true);
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Transforms in a given scene to a client connection
	IEnumerator InitializeTransformsForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		var objCount = 0;
		CustomNetTransform[] scripts = FindObjectsOfType<CustomNetTransform>();
		for (var i = 0; i < scripts.Length; i++)
		{
			if (scripts[i] == null ||
			    scripts[i].gameObject.scene != sceneContext) continue;
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null) scripts[i].NotifyPlayer(playerOwnedObj);
			//We are trying to limit physics 2d spikes on the client
			//20 notifys a frame:
			objCount += 1;
			if (objCount >= 20)
			{
				objCount = 0;
				yield return WaitFor.EndOfFrame;
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Players and sprites in a given scene to a client connection
	IEnumerator InitializePlayersForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//All player bodies
		PlayerSync[] playerBodies = FindObjectsOfType<PlayerSync>();
		for (var i = 0; i < playerBodies.Length; i++)
		{
			if (playerBodies[i].gameObject.scene != sceneContext) continue;
			var playerBody = playerBodies[i];
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null) playerBody.NotifyPlayer(playerOwnedObj, true);
			var playerSprites = playerBody.GetComponent<PlayerSprites>();
			if (playerSprites)
			{
				if (playerOwnedObj != null) playerSprites.NotifyPlayer(playerOwnedObj);
			}
			var equipment = playerBody.GetComponent<Equipment>();
			if (equipment)
			{
				if (playerOwnedObj != null) equipment.NotifyPlayer(playerOwnedObj);
			}
		}

		yield return WaitFor.EndOfFrame;
	}

	//Force a notify update for all Doors in a given scene to a client connection
	IEnumerator InitializeDoorsForClient(NetworkConnection connToAdd, Scene sceneContext)
	{
		//Doors
		DoorController[] doors = FindObjectsOfType<DoorController>();
		for (var i = 0; i < doors.Length; i++)
		{
			if (doors[i].gameObject.scene != sceneContext) continue;
			var playerOwnedObj = ClientOwnedObject(connToAdd);
			if (playerOwnedObj != null) doors[i].NotifyPlayer(playerOwnedObj);
		}

		yield return WaitFor.EndOfFrame;
	}

	public GameObject ClientOwnedObject(NetworkConnection conn)
	{
		if (conn == null || conn.clientOwnedObjects.Count == 0) return null;
		return conn.clientOwnedObjects.ElementAt(0).gameObject;
	}
}

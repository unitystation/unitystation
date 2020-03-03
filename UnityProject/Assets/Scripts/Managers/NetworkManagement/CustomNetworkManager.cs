using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using ConnectionConfig = UnityEngine.Networking.ConnectionConfig;
using Mirror;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using UnityEngine.Events;

public class CustomNetworkManager : NetworkManager
{
	public static bool IsServer => Instance._isServer;

	public static CustomNetworkManager Instance;

	[HideInInspector] public bool _isServer;
	public GameObject humanPlayerPrefab;
	public GameObject ghostPrefab;
	public GameObject disconnectedViewerPrefab;

	/// <summary>
	/// Invoked client side when the player has disconnected from a server.
	/// </summary>
	[NonSerialized]
	public UnityEvent OnClientDisconnected = new UnityEvent();

	private void Awake()
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

	private void Start()
	{
		SetSpawnableList();

		//Automatically host if starting up game *not* from lobby
		if (SceneManager.GetActiveScene().name != offlineScene)
		{
			StartHost();
		}
	}

	private void SetSpawnableList()
	{
		spawnPrefabs.Clear();

		NetworkIdentity[] networkObjects = Resources.LoadAll<NetworkIdentity>("Prefabs");
		foreach (NetworkIdentity netObj in networkObjects)
		{
			spawnPrefabs.Add(netObj.gameObject);
		}

		string[] dirs = Directory.GetDirectories(Application.dataPath, "Resources/Prefabs", SearchOption.AllDirectories); //could be changed later not to load everything to save start-up times
		foreach (string dir in dirs)
		{
			//Should yield For a frame to Increase performance
			loadFolder(dir);
			foreach (string subdir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
			{
				loadFolder(subdir);
			}
		}
	}

	private void loadFolder(string folderpath)
	{
		folderpath = folderpath.Substring(folderpath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
		foreach (NetworkIdentity netObj in Resources.LoadAll<NetworkIdentity>(folderpath))
		{
			if (!spawnPrefabs.Contains(netObj.gameObject))
			{
				spawnPrefabs.Add(netObj.gameObject);
			}
		}
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnLevelFinishedLoading;
	}

	public override void OnStartServer()
	{
		_isServer = true;
		base.OnStartServer();
		this.RegisterServerHandlers();
		// Fixes loading directly into the station scene
		if (GameManager.Instance.LoadedDirectlyToStation)
		{
			GameManager.Instance.PreRoundStart();
		}
	}

	//called on server side when player is being added, this is the main entry point for a client connecting to this server
	public override void OnServerAddPlayer(NetworkConnection conn)
	{
		if (isHeadless || GameData.Instance.testServer)
		{
			if (conn == NetworkServer.localConnection)
			{
				Logger.Log("Prevented headless server from spawning a player", Category.Server);
				return;
			}
		}

		Logger.LogFormat("Client connecting to server {0}", Category.Connections, conn);
		base.OnServerAddPlayer(conn);
		UpdateRoundTimeMessage.Send(GameManager.Instance.stationTime.ToString("O"));
	}

	//called on client side when client first connects to the server
	public override void OnClientConnect(NetworkConnection conn)
	{
		Logger.LogFormat("We (the client) connected to the server {0}", Category.Connections, conn);
		//Does this need to happen all the time? OnClientConnect can be called multiple times
		this.RegisterClientHandlers(conn);

		base.OnClientConnect(conn);
	}

	public override void OnClientDisconnect(NetworkConnection conn)
	{
		base.OnClientDisconnect(conn);
		OnClientDisconnected.Invoke();
	}

	///Sync some position data explicitly, if it is required
	/// Warning: sending a lot of data, make sure client receives it only once
	public void SyncPlayerData(GameObject playerGameObject)
	{
		Logger.LogFormat("SyncPlayerData (the big one). This server sending a bunch of sync data to new client {0}", Category.Connections, playerGameObject);
		//All matrices
		MatrixMove[] matrices = FindObjectsOfType<MatrixMove>();
		for (var i = 0; i < matrices.Length; i++)
		{
			matrices[i].NotifyPlayer(playerGameObject, true);
		}

		//All transforms
		CustomNetTransform[] scripts = FindObjectsOfType<CustomNetTransform>();
		for (var i = 0; i < scripts.Length; i++)
		{
			scripts[i].NotifyPlayer(playerGameObject);
		}

		//All player bodies
		PlayerSync[] playerBodies = FindObjectsOfType<PlayerSync>();
		for (var i = 0; i < playerBodies.Length; i++)
		{
			var playerBody = playerBodies[i];
			playerBody.NotifyPlayer(playerGameObject, true);
			var playerSprites = playerBody.GetComponent<PlayerSprites>();
			if (playerSprites)
			{
				playerSprites.NotifyPlayer(playerGameObject);
			}
			var equipment = playerBody.GetComponent<Equipment>();
			if (equipment)
			{
				equipment.NotifyPlayer(playerGameObject);
			}
		}
		//TileChange Data
		TileChangeManager[] tcManagers = FindObjectsOfType<TileChangeManager>();
		for (var i = 0; i < tcManagers.Length; i++)
		{
			tcManagers[i].NotifyPlayer(playerGameObject);
		}

		//Doors
		DoorController[] doors = FindObjectsOfType<DoorController>();
		for (var i = 0; i < doors.Length; i++)
		{
			doors[i].NotifyPlayer(playerGameObject);
		}
		Logger.Log($"Sent sync data ({matrices.Length} matrices, {scripts.Length} transforms, {playerBodies.Length} players) to {playerGameObject.name}", Category.Connections);
	}

	/// server actions when client disconnects
	public override void OnServerDisconnect(NetworkConnection conn)
	{
		//register them as removed from our own player list
		PlayerList.Instance.Remove(conn);

		//NOTE: We don't call the base.OnServerDisconnect method because it destroys the player object -
		//we want to keep the object around so player can rejoin and reenter their body.

		//note that we can't remove authority from player owned objects, the workaround is to transfer authority to
		//a different temporary object, remove authority from the original, and then run the normal disconnect logic

		//transfer to a temporary object
		GameObject disconnectedViewer = Instantiate(CustomNetworkManager.Instance.disconnectedViewerPrefab);
		NetworkServer.ReplacePlayerForConnection(conn, disconnectedViewer, System.Guid.NewGuid());

		//now we can call mirror's normal disconnect logic, which will destroy all the player's owned objects
		//which will preserve their actual body because they no longer own it
		base.OnServerDisconnect(conn);
	}

	private void OnLevelFinishedLoading(Scene oldScene, Scene newScene)
	{
		if (newScene.name != "Lobby")
		{
			//INGAME:
			// TODO check if this is needed
			// EventManager.Broadcast(EVENT.RoundStarted);
			StartCoroutine(DoHeadlessCheck());

		}
	}

	private IEnumerator DoHeadlessCheck()
	{
		yield return WaitFor.Seconds(0.1f);
		if (!GameData.IsHeadlessServer && !GameData.Instance.testServer)
		{
			if (!IsClientConnected())
			{
				//				if (GameData.IsInGame) {
				//					UIManager.Display.logInWindow.SetActive(true);
				//				}
				UIManager.Display.jobSelectWindow.SetActive(false);
			}
		}
		else
		{
			//Set up for headless mode stuff here
			//Useful for turning on and off components
			_isServer = true;
		}
	}

	public override void OnApplicationQuit()
	{
		base.OnApplicationQuit();
		// stop transport (e.g. to shut down threads)
		// (when pressing Stop in the Editor, Unity keeps threads alive
		//  until we press Start again. so if Transports use threads, we
		//  really want them to end now and not after next start)
		TelepathyTransport telepathy = GetComponent<TelepathyTransport>();
		telepathy.Shutdown();
	}

	//Editor item transform dance experiments
#if UNITY_EDITOR
	public void MoveAll()
	{
		StartCoroutine(TransformWaltz());
	}

	private IEnumerator TransformWaltz()
	{
		CustomNetTransform[] scripts = FindObjectsOfType<CustomNetTransform>();
		var sequence = new []
		{
			Vector3.right, Vector3.up, Vector3.left, Vector3.down,
				Vector3.down, Vector3.left, Vector3.up, Vector3.right
		};
		for (var i = 0; i < sequence.Length; i++)
		{
			for (var j = 0; j < scripts.Length; j++)
			{
				NudgeTransform(scripts[j], sequence[i]);
			}
			yield return WaitFor.Seconds(1.5f);
		}
	}

	private static void NudgeTransform(CustomNetTransform netTransform, Vector3 where)
	{
		netTransform.SetPosition(netTransform.ServerState.Position + where);
	}
#endif
}
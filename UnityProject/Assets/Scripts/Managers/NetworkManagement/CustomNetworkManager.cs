using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using DatabaseAPI;

public class CustomNetworkManager : NetworkManager
{
	public static bool IsServer => Instance._isServer;

	public static CustomNetworkManager Instance;

	[HideInInspector] public bool _isServer;
	[HideInInspector] private ServerConfig config;
	public GameObject humanPlayerPrefab;
	public GameObject ghostPrefab;
	public GameObject disconnectedViewerPrefab;

	private Dictionary<string, DateTime> connectCoolDown = new Dictionary<string, DateTime>();
	private const double minCoolDown = 1f;

	/// <summary>
	/// Invoked client side when the player has disconnected from a server.
	/// </summary>
	[NonSerialized]
	public UnityEvent OnClientDisconnected = new UnityEvent();

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

	public override void Start()
	{
		CheckTransport();
		ApplyConfig();
		//Automatically host if starting up game *not* from lobby
		if (SceneManager.GetActiveScene().name != "Lobby")
		{
			StartHost();
		}
	}

	void CheckTransport()
	{
		var booster = GetComponent<BoosterTransport>();
		if (booster != null)
		{
			if (transport == booster)
			{
				var beamPath = Path.Combine(Application.streamingAssetsPath, "booster.bytes");
				if (File.Exists(beamPath))
				{
					booster.beamData = File.ReadAllBytes(beamPath);
					Logger.Log("Beam data found, loading booster transport..");
				}
				else
				{
					var telepathy = GetComponent<TelepathyTransport>();
					if (telepathy != null)
					{
						Logger.Log("No beam data found. Falling back to Telepathy");
						transport = telepathy;
					}
				}
			}
		}
	}

	void ApplyConfig()
	{
		config = ServerData.ServerConfig;
		if (config.ServerPort != 0 && config.ServerPort <= 65535)
		{
			Logger.LogFormat("ServerPort defined in config: {0}", Category.Server, config.ServerPort);
			var booster = GetComponent<BoosterTransport>();
			if (booster != null)
			{
				booster.port = (ushort)config.ServerPort;
			}
			else
			{
				var telepathy = GetComponent<TelepathyTransport>();
				if (telepathy != null)
				{
					telepathy.port = (ushort)config.ServerPort;
				}
			}
		}
	}

	public void SetSpawnableList()
	{
		spawnPrefabs.Clear();

		NetworkIdentity[] networkObjects = Resources.LoadAll<NetworkIdentity>("Prefabs");
		foreach (NetworkIdentity netObj in networkObjects)
		{
			spawnPrefabs.Add(netObj.gameObject);
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
		NetworkManagerExtensions.RegisterServerHandlers();
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
		SubSceneManager.Instance.AddNewObserverScenePermissions(conn);
		UpdateRoundTimeMessage.Send(GameManager.Instance.stationTime.ToString("O"));
	}

	//called on client side when client first connects to the server
	public override void OnClientConnect(NetworkConnection conn)
	{
		Logger.LogFormat("We (the client) connected to the server {0}", Category.Connections, conn);
		//Does this need to happen all the time? OnClientConnect can be called multiple times
		NetworkManagerExtensions.RegisterClientHandlers();

		base.OnClientConnect(conn);
	}

	public override void OnClientDisconnect(NetworkConnection conn)
	{
		base.OnClientDisconnect(conn);
		OnClientDisconnected.Invoke();
	}

	public override void OnServerConnect(NetworkConnection conn)
	{
		if (!connectCoolDown.ContainsKey(conn.address))
		{
			connectCoolDown.Add(conn.address, DateTime.Now);
		}
		else
		{
			var totalSeconds = (DateTime.Now - connectCoolDown[conn.address]).TotalSeconds;
			if (totalSeconds < minCoolDown)
			{
				Logger.Log($"Connect spam alert. Address {conn.address} is trying to spam connections");
				conn.Disconnect();
				return;
			}

			connectCoolDown[conn.address] = DateTime.Now;
		}
		base.OnServerConnect(conn);
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
		if (GameData.IsHeadlessServer && GameData.Instance.testServer)
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
		var transport = GetComponent<Transport>();
		transport.Shutdown();
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using DatabaseAPI;
using IgnoranceTransport;
using Initialisation;
using Messages.Server;
using UnityEditor;

public class CustomNetworkManager : NetworkManager, IInitialise
{
	public static bool IsServer => Instance._isServer;

	// NetworkManager.isHeadless is removed in latest versions of Mirror,
	// so we assume headless would be running in batch mode.
	public static bool IsHeadless => Application.isBatchMode;

	public static CustomNetworkManager Instance;

	[HideInInspector] public bool _isServer;
	[HideInInspector] private ServerConfig config;
	public GameObject humanPlayerPrefab;
	public GameObject ghostPrefab;
	public GameObject disconnectedViewerPrefab;

	/// <summary>
	/// List of ALL prefabs in the game which can be spawned, networked or not.
	/// use spawnPrefabs to get only networked prefabs
	/// </summary>
	[HideInInspector] public List<GameObject> allSpawnablePrefabs = new List<GameObject>();

	public Dictionary<GameObject, int> IndexLookupSpawnablePrefabs = new Dictionary<GameObject, int>();

	public Dictionary<string, GameObject> ForeverIDLookupSpawnablePrefabs = new Dictionary<string, GameObject>();

	private Dictionary<string, DateTime> connectCoolDown = new Dictionary<string, DateTime>();
	private const double minCoolDown = 1f;

	/// <summary>
	/// Invoked client side when the player has disconnected from a server.
	/// </summary>
	[NonSerialized] public UnityEvent OnClientDisconnected = new UnityEvent();

	public override void Awake()
	{
		if (IndexLookupSpawnablePrefabs.Count == 0)
		{
			new Task(SetUpSpawnablePrefabsIndex).Start();
		}


		if (Instance == null)
		{
			Instance = this;
		}
		else
		{
			Destroy(gameObject);
		}
	}

	private int CurrentLocation = 0;

	public void Update()
	{
		if (allSpawnablePrefabs.Count > CurrentLocation)
		{
			for (int i = 0; i < 50; i++)
			{
				if (allSpawnablePrefabs.Count > CurrentLocation + i)
				{
					if (allSpawnablePrefabs[CurrentLocation + i] == null) continue;
					if (allSpawnablePrefabs[CurrentLocation + i].TryGetComponent<PrefabTracker>(out var PrefabTracker))
					{
						ForeverIDLookupSpawnablePrefabs[PrefabTracker.ForeverID] =
							allSpawnablePrefabs[CurrentLocation + i];
					}
				}
			}

			CurrentLocation = CurrentLocation + 50;
		}
	}

	public void SetUpSpawnablePrefabsIndex()
	{
		for (int i = 0; i < allSpawnablePrefabs.Count; i++)
		{
			IndexLookupSpawnablePrefabs[allSpawnablePrefabs[i]] = i;
		}
	}

	public void SetUpSpawnablePrefabsForEverID()
	{
		for (int i = 0; i < allSpawnablePrefabs.Count; i++)
		{
		}
	}

	public InitialisationSystems Subsystem => InitialisationSystems.CustomNetworkManager;

	void IInitialise.Initialise()
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
		// var booster = GetComponent<BoosterTransport>();
		// if (booster != null)
		// {
		// 	if (transport == booster)
		// 	{
		// 		var beamPath = Path.Combine(Application.streamingAssetsPath, "booster.bytes");
		// 		if (File.Exists(beamPath))
		// 		{
		// 			booster.beamData = File.ReadAllBytes(beamPath);
		// 			Logger.Log("Beam data found, loading booster transport..");
		// 		}
		// 		else
		// 		{
		// 			var telepathy = GetComponent<TelepathyTransport>();
		// 			if (telepathy != null)
		// 			{
		// 				Logger.Log("No beam data found. Falling back to Telepathy");
		// 				transport = telepathy;
		// 			}
		// 		}
		// 	}
		// }
	}

	void ApplyConfig()
	{
		config = ServerData.ServerConfig;
		if (config.ServerPort != 0 && config.ServerPort <= 65535)
		{
			Logger.LogFormat("ServerPort defined in config: {0}", Category.Server, config.ServerPort);
			// var booster = GetComponent<BoosterTransport>();
			// if (booster != null)
			// {
			// 	booster.port = (ushort)config.ServerPort;
			// }
			// else
			// {
			//
			// }

			var telepathy = GetComponent<TelepathyTransport>();
			if (telepathy != null)
			{
				telepathy.port = (ushort) config.ServerPort;
			}

			var ignorance = GetComponent<Ignorance>();
			if (ignorance != null)
			{
				ignorance.port = (ushort) config.ServerPort;
			}
		}
	}

	public void SetSpawnableList()
	{
#if UNITY_EDITOR
		AssetDatabase.StartAssetEditing();
		spawnPrefabs.Clear();
		allSpawnablePrefabs.Clear();

		Dictionary<string, PrefabTracker> StoredIDs = new Dictionary<string, PrefabTracker>();

		var networkObjectsGUIDs = AssetDatabase.FindAssets("t:prefab", new string[] {"Assets/Prefabs"});
		var objectsPaths = networkObjectsGUIDs.Select(AssetDatabase.GUIDToAssetPath);
		foreach (var objectsPath in objectsPaths)
		{
			var asset = AssetDatabase.LoadAssetAtPath<GameObject>(objectsPath);
			if (asset == null) continue;

			if (asset.TryGetComponent<NetworkIdentity>(out _) && playerPrefab != asset)
			{
				spawnPrefabs.Add(asset);
			}

			allSpawnablePrefabs.Add(asset);

			if (asset.TryGetComponent<PrefabTracker>(out var prefabTracker))
			{
				if (StoredIDs.ContainsKey(prefabTracker.ForeverID))
				{
					var OriginalOldID = prefabTracker.ForeverID;
					//TODO Someone smarter than me work out which one is the base prefab
					StoredIDs[prefabTracker.ForeverID].ReassignID();
					prefabTracker.ReassignID();
					var Preexisting = StoredIDs[OriginalOldID];

					if (Preexisting.ForeverID != OriginalOldID &&
					    prefabTracker.ForeverID != OriginalOldID)
					{
						Logger.LogError("OH GOD What is the original I can't tell!! " +
						                "Manually edit the ForeverID For the newly created prefab to not be the same as " +
						                "the prefab variant parent for " +
						                Preexisting.gameObject +
						                " and " + prefabTracker.gameObject);

						prefabTracker.ForeverID = OriginalOldID;
						Preexisting.ForeverID = OriginalOldID;
						continue;
					}


					StoredIDs[Preexisting.ForeverID] = Preexisting;
					StoredIDs[prefabTracker.ForeverID] = prefabTracker;
					PrefabUtility.SavePrefabAsset(Preexisting.gameObject);
					PrefabUtility.SavePrefabAsset(prefabTracker.gameObject);
				}

				StoredIDs[prefabTracker.ForeverID] = prefabTracker;
			}
		}

		AssetDatabase.StopAssetEditing();
		AssetDatabase.SaveAssets();
#endif
	}

	public GameObject GetSpawnablePrefabFromName(string prefabName)
	{
		var prefab = allSpawnablePrefabs.Where(o => o.name == prefabName).ToList();

		if (prefab.Any())
		{
			if (prefab.Count > 1)
			{
				Logger.LogError($"There is {prefab.Count} prefabs with the name: {prefabName}, please rename them");
			}

			return prefab[0];
		}

		Logger.LogError(
			$"There is no prefab with the name: {prefabName} inside the AllSpawnablePrefabs list in the network manager," +
			" all prefabs must be in this list if they need to be spawnable");

		return null;
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

	public override void OnStartHost()
	{
		StartCoroutine(WaitForInitialisation());
	}

	public IEnumerator WaitForInitialisation()
	{
		yield return null;
		yield return null;
		yield return null;
		AddressableCatalogueManager.LoadHostCatalogues();
	}

	public override void OnStartClient()
	{
		if (AddressableCatalogueManager.Instance == null) return;

		AddressableCatalogueManager.Instance.LoadClientCatalogues();
	}

	//called on server side when player is being added, this is the main entry point for a client connecting to this server
	public override void OnServerAddPlayer(NetworkConnection conn)
	{
		if (IsHeadless || GameData.Instance.testServer)
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
				Logger.Log($"Connect spam alert. Address {conn.address} is trying to spam connections",
					Category.Connections);
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
		PlayerList.Instance.RemoveByConnection(conn);

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
		var sequence = new[]
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
		netTransform.SetPosition(netTransform.ServerState.LocalPosition + where);
	}
#endif
}
using System;
using System.Collections;
using System.IO;
using Facepunch.Steamworks;
using PlayGroup;
using UI;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class CustomNetworkManager : NetworkManager
{
	public static CustomNetworkManager Instance;
	[HideInInspector] public bool _isServer;
	[HideInInspector] public bool spawnableListReady;
	public bool SteamServer = true;
	private Server server;



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
		customConfig = true;

		SetSpawnableList();
		if (!IsClientConnected() && !GameData.IsHeadlessServer &&
		    GameData.IsInGame)
		{
			UIManager.Display.logInWindow.SetActive(true);
		}

		channels.Add(QosType.ReliableSequenced);

		connectionConfig.AcksType = ConnectionAcksType.Acks64;
		connectionConfig.FragmentSize = 512;

		if(GameData.IsInGame && PoolManager.Instance == null){
			ObjectManager.StartPoolManager();
		}
	}

	private void SetSpawnableList()
	{
		spawnPrefabs.Clear();

		NetworkIdentity[] networkObjects = Resources.LoadAll<NetworkIdentity>("");
		foreach (NetworkIdentity netObj in networkObjects)
		{
			if (!netObj.gameObject.name.Contains("Player"))
			{
				spawnPrefabs.Add(netObj.gameObject);
			}
		}

		string[] dirs = Directory.GetDirectories(Application.dataPath, "Resources", SearchOption.AllDirectories);


		foreach (string dir in dirs)
		{
			loadFolder(dir);
			foreach (string subdir in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
			{
				loadFolder(subdir);
			}
		}

		spawnableListReady = true;
	}

	private void loadFolder(string folderpath)
	{
		folderpath = folderpath.Substring(folderpath.IndexOf("Resources", StringComparison.Ordinal) + "Resources".Length);
		foreach (NetworkIdentity netObj in Resources.LoadAll<NetworkIdentity>(folderpath))
		{
			if (!netObj.name.Contains("Player") && !spawnPrefabs.Contains(netObj.gameObject))
			{
				spawnPrefabs.Add(netObj.gameObject);
			}
		}
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		if (_isServer && server != null && server.IsValid)
		{
			server.Auth.OnAuthChange += AuthChange;
		}
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
	}

	public override void OnStartServer()
	{
		_isServer = true;
		base.OnStartServer();
		this.RegisterServerHandlers();
		if (SteamServer)
		{
			SteamServerStart();
		}
	}
	
	public void SteamServerStart()
	{
		// init the SteamServer needed for authentication of players
		//		
		Config.ForUnity( Application.platform.ToString() );
		string path = Path.GetFullPath(".");
		string folderName = Path.GetFileName(Path.GetDirectoryName( path ) );
		ServerInit options = new ServerInit(folderName, "Unitystation");
		server = new Server(787180, options);

		if (server != null)
		{
			if (GameData.IsHeadlessServer || GameData.Instance.testServer || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
			{
				server.DedicatedServer = true;
			}
			server.LogOnAnonymous();
			server.ServerName = "Unitystation Official";
			// Set required settings for dedicated server

			Debug.Log("Setting up Auth hook");
			//Process callback data for authentication
			server.Auth.OnAuthChange = AuthChange;
		}
		// confirm in log if server is actually registered or not
		if (server.IsValid)
		{
			Debug.Log("Server registered");
		}
		else
		{
			Debug.Log("Server NOT registered");
		}

	}
	
	/// Processes the callback data when authentication statuses change
	public void AuthChange(ulong steamid, ulong ownerid, ServerAuth.Status status)
	{
		var player = PlayerList.Instance.Get(steamid);
		if ( player == ConnectedPlayer.Invalid )
		{
			Debug.LogWarning( $"Steam gave us a {status} ticket response for unconnected id {steamid}" );
			return;
		}

		if ( status == ServerAuth.Status.OK )
		{
			Debug.LogWarning( $"Steam gave us a 'ok' ticket response for already connected id {steamid}" );
			return;
		}

		if ( status == ServerAuth.Status.VACCheckTimedOut )
		{
			return;
		}

		Kick( player, $"Steam: {status}" );
	}
	
	public static void Kick( ConnectedPlayer player, string raisins="4 no raisins" )
	{
		if ( !player.Connection.isConnected )
		{
			Debug.Log($"Not kicking, already disconnected: {player}");
			return;
		}
		Debug.Log( $"Kicking {player} : {raisins}" );
		InfoWindowMessage.Send(player.GameObject, $"Kicked: {raisins}", "Kicked");
		PostToChatMessage.Send($"Player '{player.Name}' got kicked: {raisins}", ChatChannel.System);
		player.Connection.Disconnect();
		player.Connection.Dispose();
	}


	public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
	{
		//This spawns the player prefab
		if (GameData.IsHeadlessServer || GameData.Instance.testServer)
		{
			//this is a headless server || testing headless (it removes the server player for localClient)
			if (conn.address != "localClient")
			{
				StartCoroutine(WaitToSpawnPlayer(conn, playerControllerId));
			}
		}
		else
		{
			//This is a host server (keep the server player as it is for the host player)
			StartCoroutine(WaitToSpawnPlayer(conn, playerControllerId));
		}


		if (_isServer)
		{
			//Tell them what the current round time is
			UpdateRoundTimeMessage.Send(GameManager.Instance.GetRoundTime);
		}
	}
	private IEnumerator WaitToSpawnPlayer(NetworkConnection conn, short playerControllerId)
	{
		yield return new WaitForSeconds(1f);
		OnServerAddPlayerInternal(conn, playerControllerId);
	}

	void Update()
	{
		// This code makes sure the steam server is updated
		if (server == null)
			return;
		try
		{
			Profiler.BeginSample("Steam Server Update");
			server.Update();
		}
		finally
		{
			Profiler.EndSample();
		}
	}

	private void OnDestroy()
	{
		// This code makes sure the steam server is disposed when the CNM is destroyed
		if (server != null)
		{
			server.Dispose();
			server = null;
		}
	}

	private void OnServerAddPlayerInternal(NetworkConnection conn, short playerControllerId)
	{
		if (playerPrefab == null)
		{
			if (!LogFilter.logError)
			{
				return;
			}
			Debug.LogError("The PlayerPrefab is empty on the NetworkManager. Please setup a PlayerPrefab object.");
		}
		else if (playerPrefab.GetComponent<NetworkIdentity>() == null)
		{
			if (!LogFilter.logError)
			{
				return;
			}
			Debug.LogError("The PlayerPrefab does not have a NetworkIdentity. Please add a NetworkIdentity to the player prefab.");
		}
		else if (playerControllerId < conn.playerControllers.Count && conn.playerControllers[playerControllerId].IsValid &&
		         conn.playerControllers[playerControllerId].gameObject != null)
		{
			if (!LogFilter.logError)
			{
				return;
			}
			Debug.LogError("There is already a player at that playerControllerId for this connections.");
		}
		else
		{
			SpawnHandler.SpawnPlayer(conn, playerControllerId);
		}
	}

	public override void OnClientConnect(NetworkConnection conn)
	{
//		if (_isServer)
//		{
//			//do special server wizardry here
//			PlayerList.Instance.Add(new ConnectedPlayer
//			{
//				Connection = conn,
//			});
//		}

		if (GameData.IsInGame && PoolManager.Instance == null)
		{
			ObjectManager.StartPoolManager();
		}

		//This client connecting to server, wait for the spawnable prefabs to register
		StartCoroutine(WaitForSpawnListSetUp(conn));
		this.RegisterClientHandlers(conn);
	}

	///Sync some position data explicitly, if it is required
	/// Warning: sending a lot of data, make sure client receives it only once
	public void SyncPlayerData(GameObject playerGameObject)
	{
		MatrixMove[] matrices = FindObjectsOfType<MatrixMove>();
		for (var i = 0; i < matrices.Length; i++) {
			matrices[i].NotifyPlayer(playerGameObject);
		}
		CustomNetTransform[] scripts = FindObjectsOfType<CustomNetTransform>();
		for (var i = 0; i < scripts.Length; i++) {
			scripts[i].NotifyPlayer(playerGameObject);
		}
		//tell player his position (required for spawning in moving ship)
		playerGameObject.GetComponent<PlayerSync>().NotifyPlayer( playerGameObject );
		Debug.LogFormat($"Sent sync data ({matrices.Length} matrices, {scripts.Length} transforms) to {playerGameObject.name}");
	}

	private IEnumerator WaitForSpawnListSetUp(NetworkConnection conn)
	{
		while (!spawnableListReady)
		{
			yield return new WaitForSeconds(1);
		}

		base.OnClientConnect(conn);
	}

	/// server actions when client disconnects 
	public override void OnServerDisconnect(NetworkConnection conn)
	{
		var player = PlayerList.Instance.Get(conn);
		if ( player.GameObject )
		{
			player.GameObject.GetComponent<PlayerNetworkActions>().DropAll(true);
		}
		Debug.Log($"Player Disconnected: {player.Name}");
		PlayerList.Instance.Remove(conn);
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		if (GameData.IsInGame && PoolManager.Instance == null)
		{
			ObjectManager.StartPoolManager();
		}

		if (IsClientConnected() && GameData.IsInGame)
		{
			//make sure login window does not show on scene changes if connected
			UIManager.Display.logInWindow.SetActive(false);
			UIManager.Display.infoWindow.SetActive(false);
			StartCoroutine(DoHeadlessCheck());
		}
		else
		{
			StartCoroutine(DoHeadlessCheck());
		}
	}

	private IEnumerator DoHeadlessCheck()
	{
		yield return new WaitForSeconds(0.1f);
		if (!GameData.IsHeadlessServer && !GameData.Instance.testServer)
		{
			if (!IsClientConnected())
			{
				if (GameData.IsInGame) {
					UIManager.Display.logInWindow.SetActive(true);
				}
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
			yield return new WaitForSeconds(1.5f);
		}
	}

	private static void NudgeTransform(CustomNetTransform netTransform, Vector3 where)
	{
		netTransform.SetPosition(netTransform.transform.localPosition + where);
	}
#endif
}
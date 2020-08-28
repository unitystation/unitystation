using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DatabaseAPI;
using DiscordWebhook;
using Mirror;
using GameConfig;

public partial class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public bool counting;
	/// <summary>
	/// The minimum number of players needed to start the pre-round countdown
	/// </summary>
	public int MinPlayersForCountdown { get; set; } = 1;

	/// <summary>
	/// How long the pre-round stage should last
	/// </summary>
	public float PreRoundTime { get; set; } = 120f;

	/// <summary>
	/// How long to wait between ending the round and starting a new one
	/// </summary>
	public float RoundEndTime { get; set; } = 60f;

	/// <summary>
	/// How long to wait between ending the round and starting a new one
	/// </summary>
	public int ShuttleDepartTime { get; set; } = 30;

	/// <summary>
	/// The current time left on the countdown timer
	/// </summary>
	public float CountdownTime { get; private set; }
	public double CountdownEndTime { get; private set; }

	/// <summary>
	/// Is respawning currently allowed? Can be set during a game to disable, such as when a nuke goes off.
	/// Reset to the server setting of RespawnAllowed when the level loads.
	/// </summary>
	public bool RespawnCurrentlyAllowed { get; set; }

	[HideInInspector] public string NextGameMode = "Random";

	/// <summary>
	/// True if the server allows respawning at round start by default.
	/// </summary>
	public bool RespawnAllowed { get; set; }

	/// <summary>
	/// True if the server allows gibbing people when they receive enough post-mortem damage.
	/// </summary>
	public bool GibbingAllowed { get; set; }

	/// <summary>
	/// If true, it will allow shuttles from dealing 9001 damage and instantly gibbing people when crashed
	/// </summary>
	public bool ShuttleGibbingAllowed { get; set; }

	/// <summary>
	/// The game mode that the server will switch to at round end if no mode or an invalid mode is selected.
	/// <summary>
	public string InitialGameMode { get; set; } = "Random";

	public Text roundTimer;

	public bool waitForStart;

	public DateTime stationTime;
	public int RoundsPerMap { get; set; } = 10;

	//Space bodies in the solar system <Only populated ServerSide>:
	//---------------------------------
	public List<MatrixMove> SpaceBodies = new List<MatrixMove>();
	private Queue<MatrixMove> PendingSpaceBodies = new Queue<MatrixMove>();
	private bool isProcessingSpaceBody = false;
	public float minDistanceBetweenSpaceBodies;

	private List<Vector3> EscapeShuttlePath = new List<Vector3>();
	private bool EscapeShuttlePathGenerated = false;

	[Header("Define the default size of all SolarSystems here:")]
	public float solarSystemRadius = 600f;
	//---------------------------------

	public CentComm CentComm;

	//whether the game was launched directly to a station, skipping lobby
	private bool loadedDirectlyToStation;
	public bool LoadedDirectlyToStation => loadedDirectlyToStation;

	public Queue<PlayerSpawnRequest> SpawnPlayerRequestQueue = new Queue<PlayerSpawnRequest>();

	private bool QueueProcessing;

	private float timeElapsedQueueCheckServer = 0;

	private const float QueueCheckTimeServer = 1f;

	public bool QuickLoad = false;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			//if loading directly to outpost station, need to call pre round start once CustomNetworkManager
			//starts because no scene change occurs to trigger it
			if (SceneManager.GetActiveScene().name != "Lobby")
			{
				loadedDirectlyToStation = true;
			}
		}
		else
		{
			Destroy(this);
		}
	}

	private void Start()
	{
		// Set up server defaults, needs to be loaded here to ensure gameConfigManager is load.
		LoadConfig();
		RespawnCurrentlyAllowed = RespawnAllowed;
		NextGameMode = InitialGameMode;
	}

	///<summary>
	/// Loads end user config settings for server defaults.
	/// If the JSON is configured incorrectly (null entry), uses default values.
	///</summary>
	// TODO: Currently, there is no data validation to ensure the config has reasonable values, need to configure setters.
	private void LoadConfig()
	{
		MinPlayersForCountdown = GameConfigManager.GameConfig.MinPlayersForCountdown;
		PreRoundTime = GameConfigManager.GameConfig.PreRoundTime;
		RoundEndTime = GameConfigManager.GameConfig.RoundEndTime;
		RoundsPerMap = GameConfigManager.GameConfig.RoundsPerMap;
		InitialGameMode = GameConfigManager.GameConfig.InitialGameMode;
		RespawnAllowed = GameConfigManager.GameConfig.RespawnAllowed;
		ShuttleDepartTime = GameConfigManager.GameConfig.ShuttleDepartTime;
		GibbingAllowed = GameConfigManager.GameConfig.GibbingAllowed;
		ShuttleGibbingAllowed = GameConfigManager.GameConfig.ShuttleGibbingAllowed;
	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
		EventManager.AddHandler(EVENT.RoundStarted, OnRoundStart);
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
		EventManager.RemoveHandler(EVENT.RoundStarted, OnRoundStart);
	}

	///<summary>
	/// This is for any space object that needs to be randomly placed in the solar system
	/// (See Asteroid.cs for example of use)
	/// Please make sure matrixMove.State.position != TransformState.HiddenPos when calling this function
	///</summary>
	public void ServerSetSpaceBody(MatrixMove mm)
	{
		if (mm.ServerState.Position == TransformState.HiddenPos)
		{
			Logger.LogError("Matrix Move is not initialized! Wait for it to be" +
				"ready before calling ServerSetSpaceBody ", Category.Server);
			return;
		}

		PendingSpaceBodies.Enqueue(mm);
	}

	IEnumerator ProcessSpaceBody(MatrixMove mm)
	{
		if (SceneManager.GetActiveScene().name == "BoxStationV1")
		{
			minDistanceBetweenSpaceBodies = 200f;
		}
		//Change this for larger maps to avoid asteroid spawning on station.
		else
		{
			minDistanceBetweenSpaceBodies = 200f;
		}

		//Fills list of Vectors all along shuttle path
		var beginning = GameManager.Instance.PrimaryEscapeShuttle.stationTeleportLocation;
		var target = GameManager.Instance.PrimaryEscapeShuttle.stationDockingLocation;


		var distance = (int)Vector2.Distance(beginning, target);

		if (!EscapeShuttlePathGenerated)//Only generated once
		{
			EscapeShuttlePath.Add(beginning);//Adds original vector
			for (int i = 0; i < (distance/50); i++)
			{
				beginning = Vector2.MoveTowards(beginning, target, 50);//Vector 50 distance apart from prev vector
				EscapeShuttlePath.Add(beginning);
			}
			EscapeShuttlePathGenerated = true;
		}


		bool validPos = false;
		while (!validPos)
		{
			Vector3 proposedPosition = RandomPositionInSolarSystem();

			bool failedChecks =
				Vector3.Distance(proposedPosition, MatrixManager.Instance.spaceMatrix.transform.parent.transform.position) <
				minDistanceBetweenSpaceBodies;

			//Make sure it is away from the middle of space matrix


			//Checks whether position is near (100 distance) any of the shuttle path vectors
			foreach (var vectors in EscapeShuttlePath)
			{
				if (Vector3.Distance(proposedPosition, vectors) < 100)
				{
					failedChecks = true;
				}
			}

			//Checks whether the other spacebodies are near
			for (int i = 0; i < SpaceBodies.Count; i++)
			{
				if (Vector3.Distance(proposedPosition, SpaceBodies[i].transform.position) < minDistanceBetweenSpaceBodies)
				{
					failedChecks = true;
				}
			}

			if (!failedChecks)
			{
				validPos = true;
				mm.SetPosition(proposedPosition);
				SpaceBodies.Add(mm);
			}
			yield return WaitFor.EndOfFrame;
		}
		yield return WaitFor.EndOfFrame;
		isProcessingSpaceBody = false;
	}

	public Vector3 RandomPositionInSolarSystem()
	{
		return (UnityEngine.Random.insideUnitCircle * solarSystemRadius).RoundToInt();
	}

	//	private void OnValidate()
	//	{
	//		if (Occupations.All(o => o.GetComponent<OccupationRoster>().Type != JobType.ASSISTANT)) //wtf is that about
	//		{
	//			Logger.LogError("There is no ASSISTANT job role defined in the the GameManager Occupation rosters");
	//		}
	//	}

	private void OnSceneChange(Scene oldScene, Scene newScene)
	{
		if (CustomNetworkManager.Instance._isServer && newScene.name != "Lobby")
		{
			PreRoundStart();
		}
		ResetStaticsOnNewRound();
	}

	/// <summary>
	/// Resets client and server side static fields to empty / round start values.
	/// If you have any static pools / caches / fields, add logic here to reset them to ensure they'll be properly
	/// cleared when a new round begins.
	/// </summary>
	private void ResetStaticsOnNewRound()
	{
		//reset pools
		Spawn._ClearPools();
		//clean up inventory system
		ItemSlot.Cleanup();
		//reset matrix init events
		NetworkedMatrix._ClearInitEvents();
	}

	public void SyncTime(string currentTime)
	{
		if (string.IsNullOrEmpty(currentTime)) return;

		if (!CustomNetworkManager.Instance._isServer)
		{
			stationTime = DateTime.ParseExact(currentTime, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			counting = true;
		}
	}

	public void ResetRoundTime()
	{
		stationTime = new DateTime().AddHours(12);
		counting = true;
		StartCoroutine(NotifyClientsRoundTime());
	}

	IEnumerator NotifyClientsRoundTime()
	{
		yield return WaitFor.EndOfFrame;
		UpdateRoundTimeMessage.Send(stationTime.ToString("O"));
	}

	private void Update()
	{
		if (!isProcessingSpaceBody && PendingSpaceBodies.Count > 0)
		{
			InitEscapeShuttle();
			isProcessingSpaceBody = true;
			StartCoroutine(ProcessSpaceBody(PendingSpaceBodies.Dequeue()));
		}

		if (waitForStart)
		{
			if (NetworkTime.time >= CountdownEndTime)
			{
				StartRound();
			}
		}
		else if (counting)
		{
			stationTime = stationTime.AddSeconds(Time.deltaTime);
			roundTimer.text = stationTime.ToString("HH:mm");
		}

		timeElapsedQueueCheckServer += Time.deltaTime;
		if (timeElapsedQueueCheckServer > QueueCheckTimeServer)
		{
			ProcessSpawnPlayerQueue();
			timeElapsedQueueCheckServer -= QueueCheckTimeServer;
		}
	}

	/// <summary>
	/// Calls the start of the preround
	/// </summary>
	public void PreRoundStart()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			// Clear up any space bodies
			SpaceBodies.Clear();
			PendingSpaceBodies = new Queue<MatrixMove>();

			CurrentRoundState = RoundState.PreRound;
			EventManager.Broadcast(EVENT.PreRoundStarted);

			// Wait for the PlayerList instance to init before checking player count
			StartCoroutine(WaitToCheckPlayers());
		}
	}

	void OnRoundStart()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			// Execute server-side OnSpawn hooks for mapped objects
			var iServerSpawns = FindObjectsOfType<MonoBehaviour>().OfType<IServerSpawn>();
			foreach (var s in iServerSpawns)
			{
				s.OnSpawnServer(SpawnInfo.Mapped(((Component)s).gameObject));
			}
		}
	}

	/// <summary>
	/// Setup the station and then begin the round for the selected game mode
	/// </summary>
	public void StartRound()
	{
		waitForStart = false;
		// Only do this stuff on the server
		if (CustomNetworkManager.Instance._isServer)
		{
			if (string.IsNullOrEmpty(NextGameMode) || NextGameMode == "Random")
			{
				SetRandomGameMode();
			}
			else
			{
				//Set game mode to the selected game mode
				SetGameMode(NextGameMode);
				//Then reset it to the default game mode set in the config for next round.
				NextGameMode = InitialGameMode;
			}

			// Game mode specific setup
			GameMode.SetupRound();

			// Standard round start setup
			stationTime = new DateTime().AddHours(12);
			counting = true;
			RespawnCurrentlyAllowed = GameMode.CanRespawn;
			StartCoroutine(WaitToInitEscape());
			StartCoroutine(WaitToStartGameMode());

			// Tell all clients that the countdown has finished
			UpdateCountdownMessage.Send(true, 0);

			CurrentRoundState = RoundState.Started;
			EventManager.Broadcast(EVENT.RoundStarted);
		}
	}

	/// <summary>
	/// Calls the end of the round which plays a sound and shows the round report. Server only
	/// </summary>
	public void EndRound()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			if (CurrentRoundState != RoundState.Started)
			{
				if (CurrentRoundState == RoundState.Ended)
				{
					Logger.Log("Cannot end round, round has already ended!", Category.Round);
				}
				else
				{
					Logger.Log("Cannot end round, round has not started yet!", Category.Round);
				}

				return;
			}

			CurrentRoundState = RoundState.Ended;
			counting = false;

			GameMode.EndRound();
			StartCoroutine(WaitForRoundRestart());

			if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !GameData.Instance.testServer)
			{
				SoundManager.Instance.PlayRandomRoundEndSound();
			}
		}
	}

	/// <summary>
	///  Waits for the specified round end time before restarting.
	/// </summary>
	private IEnumerator WaitForRoundRestart()
	{
		Logger.Log($"Waiting {RoundEndTime} seconds to restart...", Category.Round);
		yield return WaitFor.Seconds(RoundEndTime);
		RestartRound();
	}

	/// <summary>
	/// Wait for the PlayerList Instance to init before checking
	/// </summary>
	private IEnumerator WaitToCheckPlayers()
	{
		while (PlayerList.Instance == null)
		{
			yield return WaitFor.EndOfFrame;
		}
		// Clear the list of ready players so they have to ready up again
		PlayerList.Instance.ClearReadyPlayers();
		CheckPlayerCount();
	}

	/// <summary>
	/// Checks if there are enough players to start the pre-round countdown
	/// </summary>
	public void CheckPlayerCount()
	{
		if (CustomNetworkManager.Instance._isServer && PlayerList.Instance.ConnectionCount >= MinPlayersForCountdown)
		{
			StartCountdown();
		}
	}

	public void StartCountdown()
	{
		// Calculate when the countdown will end relative to the NetworkTime
		CountdownEndTime = NetworkTime.time + PreRoundTime;
		waitForStart = true;

		string msg = GameManager.Instance.SecretGameMode ? "Secret" : $"{GameManager.Instance.GameMode}";

		string message = $"A new round is starting on {ServerData.ServerConfig.ServerName}.\nThe current gamemode is: {msg}\nThe current map is: {SubSceneManager.ServerChosenMainStation}\n";

		var playerNumber = PlayerList.Instance.ConnectionCount > PlayerList.LastRoundPlayerCount
			? PlayerList.Instance.ConnectionCount
			: PlayerList.LastRoundPlayerCount;

		if (playerNumber == 1)
		{
			message += "There is 1 player online.\n";
		}
		else
		{
			message += $"There are {playerNumber} players online.\n";
		}

		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookAnnouncementURL, message, "");

		DiscordWebhookMessage.Instance.AddWebHookMessageToQueue(DiscordWebhookURLs.DiscordWebhookOOCURL, "\n	A new round has started		\n", "");

		UpdateCountdownMessage.Send(waitForStart, PreRoundTime);
	}

	public void ProcessSpawnPlayerQueue()
	{
		if (QueueProcessing) return;

		QueueProcessing = true;

		var count = SpawnPlayerRequestQueue.Count;

		if (count == 0)
		{
			QueueProcessing = false;
			return;
		}

		for(var i = 1; i <= count; i++)
		{
			var player = SpawnPlayerRequestQueue.Peek();

			if (player == null || player.JoinedViewer == null)
			{
				SpawnPlayerRequestQueue.Dequeue();
				continue;
			}

			int slotsTaken = GameManager.Instance.GetOccupationsCount(player.RequestedOccupation.JobType);
			int slotsMax = GameManager.Instance.GetOccupationMaxCount(player.RequestedOccupation.JobType);
			if (slotsTaken >= slotsMax)
			{
				SpawnPlayerRequestQueue.Dequeue();
				continue;
			}

			//regardless of their chosen occupation, they might spawn as an antag instead.
			//If they do, bypass the normal spawn logic.
			if (GameManager.Instance.TrySpawnAntag(player))
			{
				SpawnPlayerRequestQueue.Dequeue();
				continue;
			}

			PlayerSpawn.ServerSpawnPlayer(player);

			SpawnPlayerRequestQueue.Dequeue();
		}

		QueueProcessing = false;
	}

	public int GetOccupationsCount(JobType jobType)
	{
		int count = 0;

		if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers.Count == 0)
		{
			return 0;
		}

		for (var i = 0; i < PlayerList.Instance.ClientConnectedPlayers.Count; i++)
		{
			var player = PlayerList.Instance.ClientConnectedPlayers[i];
			if (player.Job == jobType)
			{
				count++;
			}
		}

		if (count != 0)
		{
			Logger.Log($"{jobType} count: {count}", Category.Jobs);
		}
		return count;
	}

	public int GetNanoTrasenCount()
	{
		if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers.Count == 0)
		{
			return 0;
		}

		int startCount = 0;

		for (var i = 0; i < PlayerList.Instance.ClientConnectedPlayers.Count; i++)
		{
			var player = PlayerList.Instance.ClientConnectedPlayers[i];
			if (player.Job != JobType.SYNDICATE && player.Job != JobType.NULL)
			{
				startCount++;
			}
		}
		return startCount;
	}

	public int GetOccupationMaxCount(JobType jobType)
	{
		return OccupationList.Instance.Get(jobType).Limit;
	}

	// Attempts to request job else assigns random occupation in order of priority
	public Occupation GetRandomFreeOccupation(JobType jobTypeRequest)
	{
		// Try to assign specific job
		if (jobTypeRequest != JobType.NULL)
		{
			var occupation = OccupationList.Instance.Get(jobTypeRequest);

			if (occupation != null)
			{
				if (occupation.Limit > GetOccupationsCount(occupation.JobType) || occupation.Limit == -1)
				{
					return occupation;
				}
			}
		}

		// No job found, get random via priority
		foreach (Occupation occupation in OccupationList.Instance.Occupations.OrderBy(o => o.Priority))
		{
			if (occupation.Limit == -1 || occupation.Limit > GetOccupationsCount(occupation.JobType))
			{
				return occupation;
			}
		}

		return OccupationList.Instance.Get(JobType.ASSISTANT);
	}

	/// <summary>
	/// Immediately restarts the round. Use RoundEnd instead to trigger normal end of round.
	/// Only called on the server.
	/// </summary>
	public void RestartRound()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			if (CurrentRoundState == RoundState.Restarting)
			{
				Logger.Log("Cannot restart round, round is already restarting!", Category.Round);
				return;
			}
			CurrentRoundState = RoundState.Restarting;
			StartCoroutine(ServerRoundRestart());
		}
	}

	IEnumerator ServerRoundRestart()
	{
		Logger.Log("Server restarting round now.", Category.Round);
		Chat.AddGameWideSystemMsgToChat("The round is now restarting...");

		//Notify all clients that the round has ended
		ServerToClientEventsMsg.SendToAll(EVENT.RoundEnded);

		yield return WaitFor.Seconds(0.2f);

		CustomNetworkManager.Instance.ServerChangeScene("OnlineScene");

		StopAllCoroutines();
	}
}

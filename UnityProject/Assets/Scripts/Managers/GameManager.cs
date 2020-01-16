using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public partial class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public bool counting;
	/// <summary>
	/// The minimum number of players needed to start the pre-round countdown
	/// </summary>
	public int MinPlayersForCountdown = 1;
	/// <summary>
	/// How long the pre-round stage should last
	/// </summary>
	public float PreRoundTime = 30f;
	public float startTime;
	public float restartTime = 10f;
	/// <summary>
	/// Is respawning currently allowed? Can be set during a game to disable, such as when a nuke goes off.
	/// Reset to the server setting of RespawnAllowed when the level loads.
	/// </summary>
	[NonSerialized]
	public bool RespawnCurrentlyAllowed;

	[HideInInspector] public string NextGameMode = "Random";

	/// <summary>
	/// Server setting - set in editor. Should not be changed in code.
	/// </summary>
	public bool RespawnAllowed;

	public Text roundTimer;

	public bool waitForStart;
	public bool waitForRestart;

	public DateTime stationTime;
	public int RoundsPerMap = 10;

	public string[] Maps = { "Assets/scenes/OutpostStation.unity" };
	//Put the scenes in the unity 3d editor.

	private int MapRotationCount = 0;
	private int MapRotationMapsCounter = 0;

	//Space bodies in the solar system <Only populated ServerSide>:
	//---------------------------------
	public List<MatrixMove> SpaceBodies = new List<MatrixMove>();
	private Queue<MatrixMove> PendingSpaceBodies = new Queue<MatrixMove>();
	private bool isProcessingSpaceBody = false;
	public float minDistanceBetweenSpaceBodies = 200f;
	[Header("Define the default size of all SolarSystems here:")]
	public float solarSystemRadius = 600f;
	//---------------------------------

	public CentComm CentComm;

	//whether the game was launched directly to a station, skipping lobby
	private bool loadedDirectlyToStation;
	public bool LoadedDirectlyToStation => loadedDirectlyToStation;

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

		//so respawn works when loading directly to outpost station
		RespawnCurrentlyAllowed = RespawnAllowed;


	}

	private void OnEnable()
	{
		SceneManager.activeSceneChanged += OnSceneChange;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnSceneChange;
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
		bool validPos = false;
		while (!validPos)
		{
			Vector3 proposedPosition = RandomPositionInSolarSystem();
			bool failedChecks =
				Vector3.Distance(proposedPosition, MatrixManager.Instance.spaceMatrix.transform.parent.transform.position) <
				minDistanceBetweenSpaceBodies;
			//Make sure it is away from the middle of space matrix

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
		stationTime = DateTime.Today.AddHours(12);
		waitForRestart = false;
		counting = true;
		restartTime = 10f;
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
			isProcessingSpaceBody = true;
			StartCoroutine(ProcessSpaceBody(PendingSpaceBodies.Dequeue()));
		}

		if (waitForRestart)
		{
			restartTime -= Time.deltaTime;
			if (restartTime <= 0f)
			{
				RestartRound();
			}
		}
		else if (waitForStart)
		{
			startTime -= Time.deltaTime;
			if (startTime <= 0f)
			{
				RoundStart();
			}
		}
		else if (counting)
		{
			stationTime = stationTime.AddSeconds(Time.deltaTime);
			roundTimer.text = stationTime.ToString("HH:mm");
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

		//wait for scene to be ready and fire mapped spawn hooks
		StartCoroutine(WaitToFireHooks());
	}

	private IEnumerator WaitToFireHooks()
	{
		//we have to wait to fire hooks until the root objects in the scene are active,
		//otherwise our attempt to find the hooks to call will always return 0.
		//TODO: Find a better way to do this, maybe there is a hook for this
		while (FindUtils.FindInterfaceImplementersInScene<IServerSpawn>().Count == 0)
		{
			yield return WaitFor.Seconds(1);
		}
		if (CustomNetworkManager.Instance._isServer)
		{
			//invoke all server + client side hooks on all objects that have them
			foreach (var serverSpawn in FindUtils.FindInterfaceImplementersInScene<IServerSpawn>())
			{
				serverSpawn.OnSpawnServer(SpawnInfo.Mapped(((Component)serverSpawn).gameObject));
			}
			Spawn._CallAllClientSpawnHooksInScene();
		}
		else
		{
			Spawn._CallAllClientSpawnHooksInScene();
		}
	}


	/// <summary>
	/// Setup the station and then begin the round for the selected game mode
	/// </summary>
	public void RoundStart()
	{
		waitForStart = false;
		// Only do this stuff on the server
		if (CustomNetworkManager.Instance._isServer)
		{
			if (string.IsNullOrEmpty(NextGameMode)
			    || NextGameMode == "Random")
			{
				SetRandomGameMode();
			}
			else
			{
				SetGameMode(NextGameMode);
				//set it back to random when it has been loaded
				//TODO set default game modes
				NextGameMode = "Random";
			}
			// Game mode specific setup
			GameMode.SetupRound();
			GameMode.StartRound();
			// TODO make job selection stuff

			// Standard round start setup
			stationTime = DateTime.Today.AddHours(12);
			counting = true;
			RespawnCurrentlyAllowed = GameMode.CanRespawn;
			StartCoroutine(WaitToInitEscape());

			CurrentRoundState = RoundState.Started;
			EventManager.Broadcast(EVENT.RoundStarted);
		}
	}

	/// <summary>
	/// Calls the end of the round. Server only
	/// </summary>
	public void RoundEnd()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			counting = false;

			// Prevents annoying sound duplicate when testing
			if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !GameData.Instance.testServer)
			{
				SoundManager.PlayNetworked("ApcDestroyed", 1f);
			}

			waitForRestart = true;
			GameMode.EndRound();
		}
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
		startTime = PreRoundTime;
		waitForStart = true;
		UpdateCountdownMessage.Send(waitForStart, startTime);
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
			if (occupation.Limit > GetOccupationsCount(occupation.JobType) || occupation.Limit == -1)
			{
				return occupation;
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

	//Only called on the server
	public void RestartRound()
	{
		waitForRestart = false;
		if (CustomNetworkManager.Instance._isServer)
		{
			StartCoroutine(ServerRoundRestart());
		}
	}

	IEnumerator ServerRoundRestart()
	{
		CurrentRoundState = RoundState.Ended;
		//Notify all clients that the round has ended
		ServerToClientEventsMsg.SendToAll(EVENT.RoundEnded);

		yield return WaitFor.Seconds(0.2f);

		CustomNetworkManager.Instance.ServerChangeScene(Maps[0]);
	}
}

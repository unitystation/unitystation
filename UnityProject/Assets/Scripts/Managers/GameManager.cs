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
	public List<GameObject> Occupations = new List<GameObject>();
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

	/// <summary>
	/// Server setting - set in editor. Should not be changed in code.
	/// </summary>
	public bool RespawnAllowed;

	public Text roundTimer;

	public GameObject StandardOutfit;
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

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
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
		if (mm.State.Position == TransformState.HiddenPos)
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
		return UnityEngine.Random.insideUnitCircle * solarSystemRadius;
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
	}

	public void SyncTime(string currentTime)
	{
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

			// Find all available game modes
			RefreshAllGameModes();

			CurrentRoundState = RoundState.PreRound;
			EventManager.Broadcast(EVENT.PreRoundStarted);

			// Wait for the PlayerList instance to init before checking player count
			StartCoroutine(WaitToCheckPlayers());
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
			// TODO hard coding gamemode for testing purposes
			SelectGameMode("NukeOps");
			// if (SecretGameMode && GameMode == null)
			// {
			// 	ChooseGameMode();
			// }
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
			PlayerList.Instance.ReportScores();
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
		GameObject jobObject = Occupations.Find(o => o.GetComponent<OccupationRoster>().Type == jobType);
		OccupationRoster job = jobObject.GetComponent<OccupationRoster>();
		return job.limit;
	}

	public JobOutfit GetOccupationOutfit(JobType jobType)
	{
		return Occupations.First(o => o.GetComponent<OccupationRoster>().Type == jobType)
			.GetComponent<OccupationRoster>().outfit.GetComponent<JobOutfit>();
	}

	// Attempts to request job else assigns random occupation in order of priority
	public JobType GetRandomFreeOccupation(JobType jobTypeRequest)
	{
		// Try to assign specific job
		if (jobTypeRequest != JobType.NULL)
		{
			foreach (GameObject jobObject in Occupations.Where(o =>
					o.GetComponent<OccupationRoster>().Type == jobTypeRequest))
			{
				OccupationRoster job = jobObject.GetComponent<OccupationRoster>();
				if (job.limit != -1)
				{
					if (job.limit > GetOccupationsCount(job.Type))
					{
						return job.Type;
					}
				}
				if (job.limit == -1)
				{
					return job.Type;
				}
			}
		}

		// No job found, get random via priority
		foreach (GameObject jobObject in Occupations.OrderBy(o => o.GetComponent<OccupationRoster>().priority))
		{
			OccupationRoster job = jobObject.GetComponent<OccupationRoster>();
			if (job.limit != -1)
			{
				if (job.limit > GetOccupationsCount(job.Type))
				{
					return job.Type;
				}
			}
			if (job.limit == -1)
			{
				return job.Type;
			}
		}

		return JobType.ASSISTANT;
	}

	public void RestartRound()
	{
		waitForRestart = false;
		if (CustomNetworkManager.Instance._isServer)
		{
			//TODO allow map change from admin portal

			CurrentRoundState = RoundState.Ended;
			EventManager.Broadcast(EVENT.RoundEnded);
			CustomNetworkManager.Instance.ServerChangeScene(Maps[0]);
		}
	}
}

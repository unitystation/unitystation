using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;

	//TODO: How to network this and change before connecting:
	public GameMode gameMode = GameMode.nukeops; //for demo
	public float RoundTime = 660f; //10 minutes nuke ops time + 1 minute shuttle escape, might have to adjust this in future with other modes.
	public bool counting;
	public List<GameObject> Occupations = new List<GameObject>();
	public float restartTime = 10f;
	/// <summary>
	/// Set on server if Respawn is Allowed
	/// </summary>
	public bool RespawnAllowed = true;

	public Text roundTimer;

	public GameObject StandardOutfit;
	public bool waitForRestart;

	public float GetRoundTime { get; private set; } = 660f;

	public int RoundsPerMap = 10;

	public string[] Maps = { "Assets/scenes/OutpostStation.unity" };
	//Put the scenes in the unity 3d editor.

	private int MapRotationCount = 0;
	private int MapRotationMapsCounter = 0;

	private bool shuttleArrivalBroadcasted = false;

	//Nuke ops:
	public bool shuttleArrived = false;

	public bool GameOver = false;

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
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnLevelFinishedLoading;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnLevelFinishedLoading;
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
				"ready before calling ServerSetSpaceBody ");
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
			bool failedChecks = false;
			//Make sure it is away from the middle of space matrix
			if (Vector3.Distance(proposedPosition,
					MatrixManager.Instance.spaceMatrix.transform.parent.transform.position) <
				minDistanceBetweenSpaceBodies)
			{
				failedChecks = true;
			}

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
			yield return YieldHelper.EndOfFrame;
		}
		yield return YieldHelper.EndOfFrame;
		isProcessingSpaceBody = false;
	}

	public Vector3 RandomPositionInSolarSystem()
	{
		return Random.insideUnitCircle * solarSystemRadius;
	}

	//	private void OnValidate() 
	//	{
	//		if (Occupations.All(o => o.GetComponent<OccupationRoster>().Type != JobType.ASSISTANT)) //wtf is that about
	//		{
	//			Logger.LogError("There is no ASSISTANT job role defined in the the GameManager Occupation rosters");
	//		}
	//	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		GetRoundTime = RoundTime;
		GameOver = false;
		RespawnAllowed = true;
		SpaceBodies.Clear();
		PendingSpaceBodies = new Queue<MatrixMove>();
		// if (scene.name != "Lobby")
		// {
		// 	SetUpGameMode();
		// }
	}

	//this could all still be used in the future for selecting traitors/culties/revs at game start:
	// private void SetUpGameMode()
	// {
	// 	if(gameMode == GameMode.nukeops){
	// 		//Show nuke opes selection
	// 		Debug.Log("TODO Set up UI for nuke ops game");
	// 	}
	// }

	public void SyncTime(float currentTime)
	{
		if (!CustomNetworkManager.Instance._isServer)
		{
			GetRoundTime = currentTime;
			if (currentTime > 0f)
			{
				counting = true;
			}
		}
	}

	public void ResetRoundTime()
	{
		GetRoundTime = RoundTime;
		waitForRestart = false;
		counting = true;
		restartTime = 10f;
		UpdateRoundTimeMessage.Send(GetRoundTime);
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
				waitForRestart = false;
				RestartRound();
			}
		}

		else if (counting)
		{
			GetRoundTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(GetRoundTime / 60).ToString("00") + ":" +
				(GetRoundTime % 60).ToString("00");
			if (GetRoundTime <= 0f)
			{
				counting = false;
				roundTimer.text = "GameOver";

				// Prevents annoying sound duplicate when testing
				if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null && !GameData.Instance.testServer)
				{
					SoundManager.Play("ApcDestroyed", 0.3f, 1f, 0f);
				}

				if (CustomNetworkManager.Instance._isServer)
				{
					waitForRestart = true;
					PlayerList.Instance.ReportScores();
				}
			}
			//Nuke ops shuttle arrival
			if (shuttleArrived == true && shuttleArrivalBroadcasted == false)
			{
				PostToChatMessage.Send("Escape shuttle has arrived! Crew has 1 minute to get on it.", ChatChannel.System);
				shuttleArrivalBroadcasted = true;
			}
		}
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
		if (CustomNetworkManager.Instance._isServer)
		{
			//TODO allow map change from admin portal
			// until then it is just OPDM on repeat:

			CustomNetworkManager.Instance.ServerChangeScene(Maps[0]);
		}
	}
}

public enum GameMode
{
	extended,
	nukeops
}
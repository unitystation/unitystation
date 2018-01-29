using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public float RoundTime = 480f;
	public bool counting;
	public List<GameObject> Occupations = new List<GameObject>();
	public float restartTime = 10f;

	public Text roundTimer;

	public GameObject StandardOutfit;
	public bool waitForRestart;

	public float GetRoundTime { get; private set; } = 480f;

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

	private void OnValidate()
	{
		if (Occupations.All(o => o.GetComponent<OccupationRoster>().Type != JobType.ASSISTANT))
		{
			Debug.LogError("There is no ASSISTANT job role defined in the the GameManager Occupation rosters");
		}
	}

	private void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
	{
		GetRoundTime = RoundTime;
	}

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
		}
	}

	public int GetOccupationsCount(JobType jobType)
	{
		int count = 0;

		if (PlayerList.Instance == null || PlayerList.Instance.ClientConnectedPlayers.Count == 0)
		{
			return 0;
		}

		for ( var i = 0; i < PlayerList.Instance.ClientConnectedPlayers.Count; i++ )
		{
			var player = PlayerList.Instance.ClientConnectedPlayers[i];
			if ( player.Job == jobType )
			{
				count++;
			}
		}

		if ( count != 0 )
		{
			Debug.Log($"{jobType} count: {count}");
		}
		return count;
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

	private void RestartRound()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			CustomNetworkManager.Instance.ServerChangeScene(SceneManager.GetActiveScene().name);
		}
	}
}
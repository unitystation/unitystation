using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private readonly float cacheTime = 480f;
    private bool counting;
    public List<GameObject> Occupations = new List<GameObject>();
    private float restartTime = 10f;

	public Text roundTimer;

	public GameObject StandardOutfit;
	private bool waitForRestart;

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
        if (scene.name == "DeathMatch" || scene.name == "OutpostDeathmatch")
        {
            counting = true;
        }
    }

    public void SyncTime(float currentTime)
    {
        GetRoundTime = currentTime;
    }

    public void SyncTimendResetCounter(float currentTime)
    {
        SyncTime(currentTime);
        counting = true;
    }

    public void ResetRoundTime()
    {
        GetRoundTime = cacheTime;
        restartTime = 10f;
        counting = true;
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

		if (counting)
		{
			GetRoundTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(GetRoundTime / 60).ToString("00") + ":" +
			                  (GetRoundTime % 60).ToString("00");
			if (GetRoundTime <= 0f)
			{
				counting = false;
				roundTimer.text = "GameOver";
				SoundManager.Play("ApcDestroyed", 0.3f, 1f, 0f);

				if (CustomNetworkManager.Instance._isServer)
				{
					PlayerList.Instance.ReportScores();
					waitForRestart = true;
				}
			}
		}
	}

	public int GetOccupationsCount(JobType jobType)
	{
		int count = 0;

		if (PlayerList.Instance == null || PlayerList.Instance.connectedPlayers.Count == 0)
		{
			return 0;
		}

		foreach (KeyValuePair<string, GameObject> player in PlayerList.Instance.connectedPlayers)
		{
			if (player.Value != null)
			{
				PlayerScript mob = player.Value.GetComponent<PlayerScript>();
				if (mob != null)
				{
					if (mob.JobType == jobType)
					{
						count++;
					}
				}
			}
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
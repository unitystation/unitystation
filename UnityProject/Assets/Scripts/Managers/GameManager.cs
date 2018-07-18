using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;

public class GameManager : MonoBehaviour
{
    private static float ROUNDTIME_CONSTANT = 600f;
    private static float SHUTTLETIME_CONSTANT = 60f;
    public static GameManager Instance;
	public float RoundTime = ROUNDTIME_CONSTANT;
    public float ShuttleTime = SHUTTLETIME_CONSTANT;
    public bool counting;
    public bool shuttlecounting;
	public List<GameObject> Occupations = new List<GameObject>();
    /// <summary>
    /// Set on server if Respawn is Allowed
    /// </summary>
    public bool RespawnAllowed = false;

	public Text roundTimer;

	public GameObject StandardOutfit;

	public float GetRoundTime { get; private set; } = ROUNDTIME_CONSTANT;
    public float GetShuttleTime { get; private set; } = SHUTTLETIME_CONSTANT;

    public int RoundsPerMap = 10;
	
	public string[] Maps = {"Assets/scenes/OutpostDeathmatch.unity", "Assets/scenes/Flashlight Deathmatch.unity"};
	
	private int MapRotationCount = 0;
	private int MapRotationMapsCounter = 0;

    private bool RoundScoreShow;

    //Put the scenes in the unity 3d editor.


    private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
            this.RoundTime = ROUNDTIME_CONSTANT;
            this.ShuttleTime = SHUTTLETIME_CONSTANT;
        }
		else
		{
			Destroy(this);
		}
	}

    public void SyncTime(float currentTime, float currentshuttleTime)
    {
        if (!CustomNetworkManager.Instance._isServer)
        {
            GetRoundTime = currentTime;
            if (currentTime > 0f)
            {
                counting = true;
                shuttlecounting = false;
                RoundScoreShow = false;
            }
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
        GetShuttleTime = ShuttleTime;
	}

    public void ResetRoundTime()
    {
        GetRoundTime = RoundTime;
        GetShuttleTime = ShuttleTime;
        counting = true;
        shuttlecounting = false;
        RoundScoreShow = false;
        UpdateRoundTimeMessage.Send(GetRoundTime);
    }

    private void Update()
	{
        if (counting)
		{
			GetRoundTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(GetRoundTime / 60).ToString("00") + ":" +
			                  (GetRoundTime % 60).ToString("00") + "ETA";
			if (GetRoundTime <= 0f)
			{
				counting = false;
                shuttlecounting = true;
                GetComponent<MatrixMove>().StopMovement();
            }
		}

        else if (shuttlecounting)
        {
            GetShuttleTime -= Time.deltaTime;
            roundTimer.text = Mathf.Floor(GetShuttleTime / 60).ToString("00") + ":" + (GetShuttleTime % 60).ToString("00") + "ETD";
            
            if (GetShuttleTime <= 0)
            {
                shuttlecounting = false;
                RestartRound();
            }

            else if (GetShuttleTime <= 10 && RoundScoreShow)
            {
                shuttlecounting = false;
                GetComponent<MatrixMove>().StartMovement();
                PlayerList.Instance.ReportScores();
                RoundScoreShow = false;
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

	public void RestartRound()
	{
		if (CustomNetworkManager.Instance._isServer)
		{
			MapRotationCount++;
			if (MapRotationCount < RoundsPerMap * Maps.Length) 
			{
				if ((MapRotationCount % RoundsPerMap) == 0) 
				{
					MapRotationMapsCounter++;
				}
			}
			else
			{
				MapRotationCount = 0;
				MapRotationMapsCounter = 0;
			}
			
			CustomNetworkManager.Instance.ServerChangeScene (Maps[MapRotationMapsCounter]);

            GetRoundTime = RoundTime;
            GetShuttleTime = ShuttleTime;
        }
	}
}

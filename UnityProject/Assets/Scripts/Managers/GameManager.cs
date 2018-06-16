using System.Collections.Generic;
using System.Linq;
using PlayGroup;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Networking;

public class GameManager : MonoBehaviour
{
	public static GameManager Instance;
	public float RoundTime = 600f;
    public float ShuttleTime = 60f;
    public bool counting;
    public bool ShuttleCounting;
	public List<GameObject> Occupations = new List<GameObject>();
	/// <summary>
	/// Set on server if Respawn is Allowed
	/// </summary>
	public bool RespawnAllowed = true;

	public Text roundTimer;

	public GameObject StandardOutfit;
	public bool waitForRestart;

	public float GetRoundTime { get; private set; }   = 600f;
    public float GetShuttleTime { get; private set; } = 60f;

	public int RoundsPerMap = 10;
	
	public string[] Maps = {"Assets/scenes/OutpostDeathmatch.unity", "Assets/scenes/Flashlight Deathmatch.unity"};
	
	private int MapRotationCount = 0;
	private int MapRotationMapsCounter = 0;

    public int LifeCount = 0;


    //Put the scenes in the unity 3d editor.


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
        GetShuttleTime = ShuttleTime;
	}

    public void SyncTime(float currentTime)
    {
        Debug.Log("TRACE: SyncTime " + currentTime);

        PostToChatMessage.Send("TRACE: SyncTime " + currentTime, ChatChannel.OOC);

        if (!CustomNetworkManager.Instance._isServer)
        {
            GetRoundTime = currentTime;
            if (currentTime > 0f)
            {
                counting = true;
                ShuttleCounting = false;
            }
        }
    }

    public void ResetRoundTime()
	{
		GetRoundTime = RoundTime;
        GetShuttleTime = ShuttleTime;
		counting = true;
        ShuttleCounting = false;
		UpdateRoundTimeMessage.Send(GetRoundTime);
	}

    public void ReportScores()
    {
        foreach (ConnectedPlayer player in PlayerList.Instance.InGamePlayers)
        {
            if (GetComponent<HealthBehaviour>().IsDead == false)
            {
                LifeCount++;
                PostToChatMessage.Send(player.Name + " has survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
            }
            if (LifeCount == 0)
            {
                PostToChatMessage.Send("No one has survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
                PostToChatMessage.Send("The nuke ops have killed everyone and won.", ChatChannel.OOC);
            }
            if (LifeCount == 1)
            {
                PostToChatMessage.Send(LifeCount + " person survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
                PostToChatMessage.Send("The nuke ops have failed to detonated the bomb.", ChatChannel.OOC);
            }
            if (LifeCount > 1)
            {
                PostToChatMessage.Send(LifeCount + " people survived the Syndicate terrorist attack on the NSS Cyberiad.", ChatChannel.OOC);
                PostToChatMessage.Send("The nuke ops have failed to detonated the bomb.", ChatChannel.OOC);
            }
        }

    }

    private void Update()
	{
		if (counting)
		{
			GetRoundTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(GetRoundTime / 60).ToString("00") + ":" +
			                  (GetRoundTime % 60).ToString("00") + " ETA";
			if (GetRoundTime <= 0f)
			{
				counting = false;
                ShuttleCounting = true;
			}
		}
        else if (ShuttleCounting)
        {
            GetShuttleTime -= Time.deltaTime;
            roundTimer.text = Mathf.Floor(GetShuttleTime / 60).ToString("00") + ":" +
                              (GetShuttleTime % 60).ToString("00") + " ETD";

            if (GetShuttleTime <= 0f)
            {
                ShuttleCounting = false;
                RestartRound();
            }
            else if (GetShuttleTime <= 10f)
            {
                ReportScores();
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
		}
	}

    public void NukeDetonateRoundEnd()
    {
        counting = false;
        ShuttleCounting = false;
        RestartRound();
    }
}

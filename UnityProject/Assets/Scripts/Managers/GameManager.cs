using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UI;
using UnityEngine.SceneManagement;
using PlayGroup;
using System;
using System.Linq;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject StandardOutfit;
    public List<GameObject> Occupations = new List<GameObject>();

    public Text roundTimer;
    private bool counting = false;
    private bool waitForRestart = false;
    private float remainingTime = 480f; //6min rounds
    private float cacheTime = 480f;
    private float restartTime = 10f;

    public float GetRoundTime
    {
        get { return remainingTime; }
    }

    void Awake()
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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnLevelFinishedLoading;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnLevelFinishedLoading;
    }

    void OnValidate()
    {
        if (Occupations.All(o => o.GetComponent<OccupationRoster>().Type != JobType.ASSISTANT))
            Debug.LogError("There is no ASSISTANT job role defined in the the GameManager Occupation rosters");
    }

    void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "DeathMatch")
        {
            counting = true;
        }
    }

    public void SyncTime(float currentTime)
    {
        remainingTime = currentTime;
    }

    public void ResetRoundTime()
    {
        remainingTime = cacheTime;
        restartTime = 10f;
        counting = true;
    }

    void Update()
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
            remainingTime -= Time.deltaTime;
            roundTimer.text = Mathf.Floor(remainingTime / 60).ToString("00") + ":" + (remainingTime % 60).ToString("00");
            if (remainingTime <= 0f)
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

        if (PlayerList.Instance == null)
            return 0;

        foreach (var player in PlayerList.Instance.connectedPlayers)
        {
            if (player.Value != null)
            {
                var mob = player.Value.GetComponent<PlayerScript>();
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

    public JobOutfit GetOccupationOutfit(JobType jobType)
    {
        return Occupations.First(o => o.GetComponent<OccupationRoster>().Type == jobType).GetComponent<OccupationRoster>().outfit.GetComponent<JobOutfit>();
    }

    // Attempts to request job else assigns random occupation in order of priority
    public JobType GetRandomFreeOccupation(JobType jobTypeRequest)
    {
        // Try to assign specific job
        if (jobTypeRequest != JobType.NULL)
        {
            foreach (GameObject jobObject in Occupations.Where(o => o.GetComponent<OccupationRoster>().Type == jobTypeRequest))
            {
                OccupationRoster job = jobObject.GetComponent<OccupationRoster>();
                if (job.limit != -1)
                    if (job.limit > GetOccupationsCount(job.Type))
                    {
                        return job.Type;
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
                if (job.limit > GetOccupationsCount(job.Type))
                {
                    return job.Type;
                }
            if (job.limit == -1)
            {
                return job.Type;
            }
        }

        return JobType.ASSISTANT;
    }

    void RestartRound()
    {
        if (CustomNetworkManager.Instance._isServer)
        {
            CustomNetworkManager.Instance.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
    }
}
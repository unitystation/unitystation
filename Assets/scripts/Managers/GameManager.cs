﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UI;
using UnityEngine.SceneManagement;
using PlayGroup;
using System;
using System.Linq;

public class GameManager : MonoBehaviour {

	public static GameManager Instance;

    public GameObject StandardOutfit;
    public List<GameObject> Occupations = new List<GameObject>();

	public Text roundTimer;
	private bool counting = false;
	private bool waitForRestart = false;
	private float remainingTime = 480f; //6min rounds
	private float cacheTime = 480f;
	private float restartTime = 10f;
	public float GetRoundTime {get{
			return remainingTime;
		}}

	void Awake(){
		if (Instance == null) {
			Instance = this;
		} else {
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

	void OnLevelFinishedLoading(Scene scene, LoadSceneMode mode){
		if (scene.name != "Lobby") {
			counting = true;
		}

	}
		
	public void SyncTime(float currentTime){
		remainingTime = currentTime;
	}

	public void ResetRoundTime(){
		remainingTime = cacheTime;
		restartTime = 10f;
		counting = true;
	}

	void Update(){
		if (!GameData.IsHeadlessServer) {
			if (Screen.width > 1024 || Screen.height > 640) {
				Screen.SetResolution(1024, 640, false);
			}
		}

		if (waitForRestart) {
			restartTime -= Time.deltaTime;
			if (restartTime <= 0f) {
				waitForRestart = false;
				RestartRound();
			}
		}

		if (counting) {
			remainingTime -= Time.deltaTime;
			roundTimer.text = Mathf.Floor(remainingTime / 60).ToString("00") + ":" + (remainingTime % 60).ToString("00");
			if (remainingTime <= 0f) {
				counting = false;
				roundTimer.text = "GameOver";
				SoundManager.Play("ApcDestroyed",0.3f,1f,0f);

				if (CustomNetworkManager.Instance._isServer) {
					PlayerList.Instance.ReportScores();
					waitForRestart = true;
				}
			}
		}

		//NOTE: Switching off for 0.1.3
		//if there are multiple people and everyone is dead besides one player, restart the match
//		if (counting && PlayerList.playerList != null && PlayerList.Instance.connectedPlayers.Count > 1) {
//			int playerCount = PlayerList.playerList.connectedPlayers.Count;
//			int deadCount = 0;
//			foreach (var player in PlayerList.playerList.connectedPlayers) {
//				if (player.Value != null) {
//					var human = player.Value.GetComponent<Human> ();
//					if (human != null) {
//						//if a player is dead or effectivly dead count them towards a dead total
//						if (human.mobStat == MobConsciousStat.DEAD) {
//							deadCount++;
//						}
//					}
//				}
//			}

			//if there is only one or less people left, restart
//			if (playerCount - deadCount <= 1) {
//				counting = false;
//				roundTimer.text = "GameOver";
//				SoundManager.Play("ApcDestroyed",0.3f,1f,0f);
//
//				if (CustomNetworkManager.Instance._isServer) {
//					PlayerList.Instance.ReportScores();
//					waitForRestart = true;
//				}
//			}
//		}
	}

    public int GetOccupationsCount(JobType jobType)
    {
        int count = 0;

        if (PlayerList.playerList == null)
            return 0;

        foreach (var player in PlayerList.playerList.connectedPlayers)
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
        return Occupations.Where(o => o.GetComponent<OccupationRoster>().Type == jobType).First().GetComponent<OccupationRoster>().outfit.GetComponent<JobOutfit>();
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
    
    void RestartRound(){
		if (CustomNetworkManager.Instance._isServer) {
			CustomNetworkManager.Instance.ServerChangeScene(SceneManager.GetActiveScene().name);
		}
	}
}

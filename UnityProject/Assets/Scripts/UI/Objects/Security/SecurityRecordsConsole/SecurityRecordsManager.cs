using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = System.Random;

public class SecurityRecordsManager : MonoBehaviour
{
	public List<SecurityRecord> SecurityRecords = new List<SecurityRecord>();

	public static SecurityRecordsManager Instance;

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
		SceneManager.activeSceneChanged += OnRoundRestart;
	}

	private void OnDisable()
	{
		SceneManager.activeSceneChanged -= OnRoundRestart;
	}

	void OnRoundRestart(Scene scene, Scene newScene)
	{
		SecurityRecords.Clear();
	}

	/// <summary>
	/// Adds record to SecurityRecords list.
	/// Called in RespawnPlayer, so every new respawn creates a record.
	/// </summary>
	public void AddRecord(PlayerScript script, JobType jobType)
	{
		SecurityRecord record = new SecurityRecord();

		record.EntryName = script.playerName;
		record.Age = script.characterSettings.Age.ToString();
		record.Rank = script.mind.occupation.JobType.JobString();
		record.Occupation = OccupationList.Instance.Get(jobType);
		record.Sex = script.characterSettings.Gender.ToString();
		//We don't have races yet. Or I didn't find them.
		record.Species = "Human";
		//I don't know what to put in ID and Fingerprints
		record.ID = $"{UnityEngine.Random.Range(111, 999).ToString()}-{UnityEngine.Random.Range(111, 999).ToString()}";
		record.Fingerprints = UnityEngine.Random.Range(111111, 999999).ToString();
		//Photo stuff
		record.characterSettings = script.characterSettings;

		SecurityRecords.Add(record);
	}

}

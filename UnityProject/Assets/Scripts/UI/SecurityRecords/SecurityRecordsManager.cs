using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class SecurityRecordsManager : MonoBehaviour
{
	public List<SecurityRecord> SecurityRecords = new List<SecurityRecord>();

	private static SecurityRecordsManager instance;

	public static SecurityRecordsManager Instance
	{
		get
		{
			if (instance == null)
			{
				instance = FindObjectOfType<SecurityRecordsManager>();
			}
			return instance;
		}
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
		record.Rank = script.mind.jobType.JobString();
		record.jobOutfit = GameManager.Instance.GetOccupationOutfit(jobType);
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

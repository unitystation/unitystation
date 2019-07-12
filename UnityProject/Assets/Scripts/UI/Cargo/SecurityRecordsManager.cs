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

	private void Start()
	{

	}

	public void AddRecord(PlayerScript script)
	{
		SecurityRecord record;

		record = new SecurityRecord();
		record.EntryName = script.playerName;
		record.Age = script.CharacterSettings.Age.ToString();
		record.Rank = script.JobType.JobString();
		record.Sex = script.CharacterSettings.Gender.ToString();
		//We don't have races yet. Or I didn't find them.
		record.Species = "Human";
		//I don't know what to put in ID and Fingerprints
		record.ID = $"{UnityEngine.Random.Range(111, 999).ToString()}-{UnityEngine.Random.Range(111, 999).ToString()}";
		record.Fingerprints = UnityEngine.Random.Range(111111, 999999).ToString();
		//Photo stuff
		record.player = script;

		SecurityRecords.Add(record);
	}

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeShuttle : MonoBehaviour {
	//Escape shuttle script for nuke ops

	public static EscapeShuttle Instance;

	public Vector2 destination; //Evac location

	public bool spawnedIn = false; //spawned in for first time
	public bool setCourse = false; //Is shuttle heading for station?
	public bool arrivedAtStation = false;

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

	void Update ()
	{
		if(GameManager.Instance.GetRoundTime <= 120f && spawnedIn == false && setCourse == false) // Warp close to station 2 min before round ends
		{
			SpawnNearStation();
			setCourse = true;
		}

		if(spawnedIn && Vector2.Distance(transform.position, destination) < 1) //If shuttle arrived
		{
			arrivedAtStation = true;
			GetComponent<MatrixMove>().StopMovement();
			GetComponent<MatrixMove>().RotateTo(Orientation.Up); //Rotate shuttle correctly so doors are facing correctly
			GameManager.Instance.shuttleArrived = true;
		}
	}

	public void SpawnNearStation()
	{
		//Picks a slightly random position for shuttle to spawn in to try avoid interception from syndicate
		GetComponent<MatrixMove>().SetPosition(Random.insideUnitCircle * 500 + new Vector2(500, -500));
		transform.position.Normalize();

		spawnedIn = true;
		ApproachStation();
	}

	public void ApproachStation()
	{
		GetComponent<MatrixMove>().SetSpeed(25);
		GetComponent<MatrixMove>().AutopilotTo(destination);
	}

	public int GetCrewCountOnboard() //Returns how many crew members (excluding syndicate) are on the shuttle
	{									// (Used to calculate if crew managed to escape at end of round)
		int crewCount = 0;
		PlayerScript[] crewMembers = FindObjectsOfType<PlayerScript>();

		foreach(PlayerScript ps in crewMembers)
		{
			if (ps.JobType != JobType.SYNDICATE && ps.gameObject.GetComponent<PlayerHealth>().Health > 0 && ps.gameObject.transform.root == gameObject.transform.root)
			{
				crewCount++;
			}
		}
		return crewCount;
	}
}

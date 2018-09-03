using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeShuttle : MonoBehaviour {
	//Escape shuttle script for nuke ops

	public static EscapeShuttle Instance;

	public Vector2 destination; //Evac location

	public int offSetReverse; //How many tiles to reverse into station.

	public bool spawnedIn = false; //spawned in for first time
	public bool setCourse = false; //Is shuttle heading for station?
	public bool arrivedAtStation = false;

	private MatrixMove mm;

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

		mm = GetComponent<MatrixMove>();
	}

	void Update ()
	{
		if(GameManager.Instance.GetRoundTime <= 180f && spawnedIn == false && setCourse == false) // Warp close to station 3 mins before round ends
		{
			SpawnNearStation();
			setCourse = true;
		}

		if(spawnedIn && setCourse && Vector2.Distance(transform.position, destination) < 2) //If shuttle arrived
		{
			arrivedAtStation = true;
			GameManager.Instance.shuttleArrived = true;
			setCourse = false;

			mm.StopMovement();
			mm.RotateTo(Orientation.Up); //Rotate shuttle correctly so doors are facing correctly
			mm.ChangeDir(Vector2.left); //Reverse into station evac doors.
			StartCoroutine(ReverseIntoStation(mm));
		}

		if (GameManager.Instance.GetRoundTime <= 60f && arrivedAtStation == true) // Depart the shuttle
		{
			mm.ChangeDir(Vector2.right);
			mm.StartMovement();
		}
	}

	IEnumerator ReverseIntoStation(MatrixMove mm)
	{
		yield return new WaitForSeconds(3f);
		mm.MoveFor(offSetReverse);
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

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
		mm.SetAccuracy(0);
	}

	void Update ()
	{
		if(GameManager.Instance.GetRoundTime <= 150f && spawnedIn == false && setCourse == false) // Warp close to station 2.5 mins before round ends
		{
			SpawnNearStation();
			setCourse = true;
		}

		if(spawnedIn && setCourse && Vector2.Distance(transform.position, destination) < 2) //If shuttle arrived
		{
			arrivedAtStation = true;
			GameManager.Instance.shuttleArrived = true;
			setCourse = false;

			mm.SetPosition(destination);

			mm.StopMovement();
			mm.RotateTo(Orientation.Right); //Rotate shuttle correctly so doors are facing correctly
			mm.ChangeDir(Orientation.Left); //Reverse into station evac doors.
			StartCoroutine(ReverseIntoStation(mm));
		}

		if (GameManager.Instance.GetRoundTime <= 30f && arrivedAtStation == true) // Depart the shuttle
		{
			mm.ChangeDir(Orientation.Right);
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
		mm.SetPosition(Random.insideUnitCircle * 500 + new Vector2(500, -500));

		spawnedIn = true;
		ApproachStation();
	}

	public void ApproachStation()
	{
		mm.SetSpeed(25);
		mm.AutopilotTo(destination);
	}

	public int GetCrewCountOnboard() //Returns how many crew members (excluding syndicate) are on the shuttle
	{									// (Used to calculate if crew managed to escape at end of round)
		int crewCount = 0;
		List<ConnectedPlayer> crewMembers = PlayerList.Instance.InGamePlayers;

		foreach (ConnectedPlayer ps in crewMembers)
		{
			if (ps.Job != JobType.SYNDICATE && ps.GameObject.GetComponent<PlayerHealth>().OverallHealth > 0 && ps.GameObject.GetComponent<PlayerSync>().ServerState.MatrixId == mm.MatrixInfo.Id)
			{
				crewCount++;
			}
		}
		return crewCount;
	}
}

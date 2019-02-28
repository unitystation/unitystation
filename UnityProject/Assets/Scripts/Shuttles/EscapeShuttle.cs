using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeShuttle : MonoBehaviour
{
	//Escape shuttle script for nuke ops

	public static EscapeShuttle Instance;

	public Vector2 destination; //Evac location

	public int offSetReverse; //How many tiles to reverse into station.

	public bool spawnedIn = false; //spawned in for first time
	public bool setCourse = false; //Is shuttle heading for station?
	public bool arrivedAtStation = false;
	private float waitAtStationTime = 0f;
	private bool departed = false;
	private float departingFlightTime = 0f;
	private bool roundEnded = false;

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

	/// <summary>
	/// Call the escape shuttle. Server only
	/// Use this method with command consoles
	/// </summary>
	public void CallEscapeShuttle()
	{
		if (!spawnedIn && !setCourse && CustomNetworkManager.Instance._isServer)
		{
			SpawnNearStation();
			setCourse = true;
		}
	}

	void Update()
	{
		if (spawnedIn && setCourse && Vector2.Distance(transform.position, destination) < 2) //If shuttle arrived
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

		if (arrivedAtStation && !departed)
		{
			waitAtStationTime += Time.deltaTime;
			if (waitAtStationTime > 60f)
			{
				DepartStation();
			}
		}

		if (departed && !roundEnded)
		{
			departingFlightTime += Time.deltaTime;
			if(departingFlightTime > 60f){
				roundEnded = true;
				GameManager.Instance.RoundEnd();
			}
		}
	}

	private void DepartStation()
	{
		departed = true;
		mm.ChangeDir(Orientation.Right);
		mm.StartMovement();
		PostToChatMessage.Send("Escape shuttle has departed. If you have been left behind, kindly turn off all the lights and dispose of yourself via the nearest airlock.", ChatChannel.System);
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
	{ // (Used to calculate if crew managed to escape at end of round)
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
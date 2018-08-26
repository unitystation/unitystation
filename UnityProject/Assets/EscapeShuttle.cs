using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EscapeShuttle : MonoBehaviour {
	//Escape shuttle script for nuke ops

	public Vector2 destination; //Evac location

	public bool spawnedIn = false; //spawned in for first time
	public bool arrivedAtStation = false;

	void Start ()
	{
		
	}
	
	void Update ()
	{
		if(GameManager.Instance.GetRoundTime <= 90f && spawnedIn == false) // Warp close to station 1.5 min before round ends
		{
			SpawnNearStation();
		}
	}

	public void SpawnNearStation()
	{
		//Picks a random position for shuttle to spawn in to try avoid interception from syndicate
		//transform.position = Random.insideUnitCircle * 500 + new Vector2(-100, -100);
		GetComponent<MatrixMove>().SetPosition(Random.insideUnitCircle * 500 + new Vector2(-100, -100));
		//transform.position.Normalize();

		spawnedIn = true;
		ApproachStation();
	}

	public void ApproachStation()
	{
		GetComponent<MatrixMove>().SetSpeed(20);
		GetComponent<MatrixMove>().AutopilotTo(destination);
		//GetComponent<MatrixMove>().
	}
}

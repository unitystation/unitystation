using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Asteroid : NetworkBehaviour {

	private MatrixMove mm;

	private float asteroidDistance = 550; //How far can asteroids be spawned

	private float distanceFromStation = 175; //Offset from station so it doesnt spawn into station

	private float startDelay = 0.25f;


	private void Start()
	{
		mm = GetComponent<MatrixMove>();

		if (isServer)
		{
			StartCoroutine(DelayedStart());
		}
	}

	[Server]
	public void SpawnNearStation()
	{
		//Makes sure asteroids don't spawn at/inside station
		Vector2 clampVal = Random.insideUnitCircle * asteroidDistance;
		
		if(clampVal.x > 0)
		{
			clampVal.x = Mathf.Clamp(clampVal.x, distanceFromStation, asteroidDistance);
		}
		else
		{
			clampVal.x = Mathf.Clamp(clampVal.x, -distanceFromStation, -asteroidDistance);
		}
		mm.SetPosition(clampVal);
	}

	[Server] //Asigns random rotation to each asteroid at startup for variety.
	public void RandomRotation()
	{
		int rand = Random.Range(0, 3);

		switch(rand)
		{
			case 0:
				mm.RotateTo(Orientation.Up);
				break;
			case 1:
				mm.RotateTo(Orientation.Down);
				break;
			case 2:
				mm.RotateTo(Orientation.Right);
				break;
			case 3:
				mm.RotateTo(Orientation.Left);
				break;
		}
	}

	//Delays start functions to avoid issues with matrixmove
	IEnumerator DelayedStart()
	{
		yield return new WaitForSeconds(startDelay);

		SpawnNearStation();
		RandomRotation();
	}

}

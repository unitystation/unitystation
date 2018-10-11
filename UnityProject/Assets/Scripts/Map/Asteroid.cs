using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Asteroid : NetworkBehaviour {

	private MatrixMove mm;

	private float asteroidDistance = 550; //How far can asteroids be spawned

	private float distanceFromStation = 175; //Offset from station so it doesnt spawn into station


	private void Start()
	{
		mm = GetComponent<MatrixMove>();

		if (isServer)
		{
			SpawnNearStation();
		}
	}

	[Server]
	public void SpawnNearStation()
	{
		//Makes sure asteroids don't spawn at/Inside station
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

}

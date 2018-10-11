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
		//Based on EscapeShuttle.cs
		mm.SetPosition(Random.insideUnitCircle * asteroidDistance + new Vector2(distanceFromStation, -distanceFromStation));
	}

}

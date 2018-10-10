using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Asteroid : NetworkBehaviour {

	private MatrixMove mm;

	private float asteroidDistance = 550; //How far can asteroids be spawned


	private void Start()
	{
		mm = GetComponent<MatrixMove>();

		SpawnNearStation();
	}

	[Server]
	public void SpawnNearStation()
	{
		if (isServer)
		{
			//Based on EscapeShuttle.cs
			mm.SetPosition(Random.insideUnitCircle * asteroidDistance);
		}
	}

}

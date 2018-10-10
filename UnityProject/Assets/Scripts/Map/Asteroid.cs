using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour {

	private MatrixMove mm;

	private float asteroidDistances = 550; //How far can asteroids be spawned


	private void Awake()
	{
		mm = GetComponent<MatrixMove>();

		SpawnNearStation();
	}

	public void SpawnNearStation()
	{
		//Based on EscapeShuttle.cs
		mm.SetPosition(Random.insideUnitCircle * 500 + new Vector2(500, -500));
	}

}

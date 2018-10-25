using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Asteroid : NetworkBehaviour
{
	private MatrixMove mm;

	private float asteroidDistance = 550; //How far can asteroids be spawned

	private float distanceFromStation = 175; //Offset from station so it doesnt spawn into station

	void OnEnable()
	{
		if (mm == null)
		{
			mm = GetComponent<MatrixMove>();
		}
	}
	public override void OnStartServer()
	{
		StartCoroutine(Init());
		base.OnStartServer();
	}

	[Server]
	public void SpawnNearStation()
	{
		//Request a position from GameManager and cache the object in SpaceBodies List
		GameManager.Instance.ServerSetSpaceBody(mm);
	}

	[Server] //Asigns random rotation to each asteroid at startup for variety.
	public void RandomRotation()
	{
		int rand = Random.Range(0, 4);

		switch (rand)
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

	//Wait for MatrixMove init on the server:
	IEnumerator Init()
	{
		while (mm.State.Position == TransformState.HiddenPos)
		{
			yield return YieldHelper.EndOfFrame;
		}
		SpawnNearStation();
		RandomRotation();
	}

}
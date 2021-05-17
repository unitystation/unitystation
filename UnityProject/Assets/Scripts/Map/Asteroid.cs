using System.Collections;
using Mirror;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Map
{
	public class Asteroid : MonoBehaviour
	{
		private MatrixMove mm;

		// TODO Find a use for these variables or delete them.
		/*
	private float asteroidDistance = 550; //How far can asteroids be spawned

	private float distanceFromStation = 175; //Offset from station so it doesnt spawn into station
	*/

		void OnEnable()
		{
			if (mm == null)
			{
				mm = GetComponent<MatrixMove>();
			}
		}

		private void Start()
		{
			if (CustomNetworkManager.IsServer)
			{
				StartCoroutine(Init());
			}
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
					mm.SteerTo(Orientation.Up);
					break;
				case 1:
					mm.SteerTo(Orientation.Down);
					break;
				case 2:
					mm.SteerTo(Orientation.Right);
					break;
				case 3:
					mm.SteerTo(Orientation.Left);
					break;
			}
		}

		//Wait for MatrixMove init on the server:
		IEnumerator Init()
		{
			while (mm.ServerState.Position == TransformState.HiddenPos)
			{
				yield return WaitFor.EndOfFrame;
			}
			SpawnNearStation();
			RandomRotation();
		}

	}
}
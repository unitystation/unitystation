using System.Collections;
using Mirror;
using TileMap.Behaviours;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Map
{
	public class Asteroid : ItemMatrixSystemInit
	{



		// TODO Find a use for these variables or delete them.
		/*
	private float asteroidDistance = 550; //How far can asteroids be spawned

	private float distanceFromStation = 175; //Offset from station so it doesnt spawn into station
	*/


		public override void Start()
		{
			base.Start();
			if (CustomNetworkManager.IsServer)
			{
				StartCoroutine(Init());
			}
		}

		[Server]
		public void SpawnNearStation()
		{
			//Request a position from GameManager and cache the object in SpaceBodies List
			GameManager.Instance.ServerSetSpaceBody(MatrixMove);
		}

		[Server] //Asigns random rotation to each asteroid at startup for variety.
		public void RandomRotation()
		{
			int rand = Random.Range(0, 4);

			 switch (rand)
			 {
			 	case 0:
				    MatrixMove.NetworkedMatrixMove.TargetOrientation = OrientationEnum.Up_By0;
			 		break;
			 	case 1:
				    MatrixMove.NetworkedMatrixMove.TargetOrientation = OrientationEnum.Down_By180;
			 		break;
			 	case 2:
				    MatrixMove.NetworkedMatrixMove.TargetOrientation = OrientationEnum.Right_By270;
			 		break;
			 	case 3:
				    MatrixMove.NetworkedMatrixMove.TargetOrientation = OrientationEnum.Left_By90;
			 		break;
			 }
		}

		//Wait for MatrixMove init on the server:
		IEnumerator Init()
		{
			yield return WaitFor.EndOfFrame;
			SpawnNearStation();
			yield return null;
			yield return null;
			yield return null;
			RandomRotation();
		}

	}
}
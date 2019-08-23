
using System.Collections;
using UnityEngine;

/// <summary>
/// Escape-related part of GameManager
/// </summary>
public partial class GameManager
{
	public EscapeShuttle PrimaryEscapeShuttle => primaryEscapeShuttle;
	[SerializeField]
	private EscapeShuttle primaryEscapeShuttle;

	/// <summary>
	/// Called after MatrixManager is initialized
	/// </summary>
	private void InitEscapeStuff()
	{
		//Primary escape shuttle lookup
		if ( !PrimaryEscapeShuttle )
		{
			var shuttles = FindObjectsOfType<EscapeShuttle>();
			if ( shuttles.Length != 1 )
			{
				Logger.LogError( "Primary escape shuttle is missing from GameManager!", Category.Round );
				return;
			}
			Logger.LogWarning( "Primary escape shuttle is missing from GameManager, but one was found on scene" );
			primaryEscapeShuttle = shuttles[0];
		}

		//later, maybe: keep a list of all computers and call the shuttle automatically with a 25 min timer if they are deleted

		//Starting up at Centcom coordinates
		PrimaryEscapeShuttle.MatrixInfo.MatrixMove.SetPosition( PrimaryEscapeShuttle.CentcomDest.Position );

		PrimaryEscapeShuttle.OnShuttleUpdate?.AddListener( status =>
		{
			if ( status == ShuttleStatus.DockedCentcom )
			{
				RoundEnd();
				Logger.Log( "Shuttle arrived to Centcom, Round should end here", Category.Round );
			}

			if (status == ShuttleStatus.DockedStation)
			{
				SoundManager.PlayNetworked( "Disembark" );
				PostToChatMessage.Send("Escape shuttle has arrived! Crew has 1 minute to get on it.", ChatChannel.System);
				//should be changed to manual send later
				StartCoroutine( SendEscapeShuttle( 60 ) );
			}
		} );
	}

	private IEnumerator SendEscapeShuttle( int seconds )
	{
		yield return WaitFor.Seconds( seconds );
		PrimaryEscapeShuttle.SendShuttle();
		yield return WaitFor.Seconds( seconds * 2 );
		PrimaryEscapeShuttle.Status = ShuttleStatus.DockedCentcom; //pretending that we docked for round to end
	}

	private IEnumerator WaitToInitEscape()
	{
		while ( !MatrixManager.IsInitialized )
		{
			yield return WaitFor.EndOfFrame;
		}
		InitEscapeStuff();
	}
}

//public struct EscapeShuttleStatus
//{
//}

// old crapcode for reference
//	public void SpawnNearStation()
//	{
//		//Picks a slightly random position for shuttle to spawn in to try avoid interception from syndicate
//		mm.SetPosition(Random.insideUnitCircle * 500 + new Vector2(500, -500));
//
//		spawnedIn = true;
//		ApproachStation();
//	}
//	void Update()
//	{
//		if (spawnedIn && setCourse && Vector2.Distance(transform.position, destination) < 2) //If shuttle arrived
//		{
//			arrivedAtStation = true;
//			GameManager.Instance.shuttleArrived = true;
//			setCourse = false;
//
//			mm.SetPosition(destination);
//
//			mm.StopMovement();
//			mm.RotateTo(Orientation.Right); //Rotate shuttle correctly so doors are facing correctly
//			mm.ChangeDir(Orientation.Left); //Reverse into station evac doors.
//			StartCoroutine(ReverseIntoStation(mm));
//		}
//
//		if (arrivedAtStation && !departed)
//		{
//			waitAtStationTime += Time.deltaTime;
//			if (waitAtStationTime > 60f)
//			{
//				DepartFromStation();
//			}
//		}
//
//		if (departed && !roundEnded)
//		{
//			departingFlightTime += Time.deltaTime;
//			if(departingFlightTime > 60f){
//				roundEnded = true;
//				GameManager.Instance.RoundEnd();
//			}
//		}
//	}
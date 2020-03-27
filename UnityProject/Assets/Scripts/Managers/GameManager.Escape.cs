
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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

		bool beenToStation = false;

		PrimaryEscapeShuttle.OnShuttleUpdate?.AddListener( status =>
		{
			//status display ETA tracking
			if ( status == ShuttleStatus.OnRouteStation )
			{
				PrimaryEscapeShuttle.OnTimerUpdate.AddListener( TrackETA );
			} else
			{
				PrimaryEscapeShuttle.OnTimerUpdate.RemoveListener( TrackETA );
				CentComm.OnStatusDisplayUpdate.Invoke( StatusDisplayChannel.EscapeShuttle, string.Empty);
			}

			if ( status == ShuttleStatus.DockedCentcom && beenToStation )
			{
				Logger.Log( "Shuttle arrived to Centcom, Round should end here", Category.Round );
				EndRound();
			}

			if (status == ShuttleStatus.DockedStation)
			{
				beenToStation = true;
				SoundManager.PlayNetworked( "Disembark" );
				Chat.AddSystemMsgToChat("<color=white>Escape shuttle has arrived! Crew has 1 minute to get on it.</color>", MatrixManager.MainStationMatrix);
				//should be changed to manual send later
				StartCoroutine( SendEscapeShuttle( 60 ) );
			}
		} );
	}

	private void TrackETA(int eta)
	{
		CentComm.OnStatusDisplayUpdate.Invoke( StatusDisplayChannel.EscapeShuttle, FormatTime( eta, "STATION\nETA: " ) );
	}

	private static string FormatTime( int timerSeconds, string prefix = "ETA: " )
	{
		if ( timerSeconds < 1 )
		{
			return string.Empty;
		}

		return prefix+TimeSpan.FromSeconds( timerSeconds ).ToString( "mm\\:ss" );
	}

	private IEnumerator SendEscapeShuttle( int seconds )
	{
		//departure countdown
		for ( int i = seconds - 1; i >= 0; i-- )
		{
			CentComm.OnStatusDisplayUpdate.Invoke( StatusDisplayChannel.EscapeShuttle, FormatTime(i, "Departing in\n") );
			yield return WaitFor.Seconds(1);
		}

		PrimaryEscapeShuttle.SendShuttle();

		//centcom round end countdown
		int timeToCentcom = (seconds * 2);
		for ( int i = timeToCentcom - 1; i >= 0; i-- )
		{
			CentComm.OnStatusDisplayUpdate.Invoke( StatusDisplayChannel.EscapeShuttle, FormatTime(i, "CENTCOM\nETA: ") );
			yield return WaitFor.Seconds(1);
		}

		CentComm.OnStatusDisplayUpdate.Invoke( StatusDisplayChannel.EscapeShuttle, string.Empty);

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
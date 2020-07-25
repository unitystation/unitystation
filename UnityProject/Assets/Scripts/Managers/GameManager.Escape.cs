
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

	private void InitEscapeShuttle ()
	{
		//Primary escape shuttle lookup
		if (!PrimaryEscapeShuttle)
		{
			var shuttles = FindObjectsOfType<EscapeShuttle>();
			if (shuttles.Length != 1)
			{
				Logger.LogError("Primary escape shuttle is missing from GameManager!", Category.Round);
				return;
			}
			Logger.LogWarning("Primary escape shuttle is missing from GameManager, but one was found on scene");
			primaryEscapeShuttle = shuttles[0];
		}
	}

	/// <summary>
	/// Called after MatrixManager is initialized
	/// </summary>
	///
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
		if (GameManager.Instance.QuickLoad)
		{
			if (primaryEscapeShuttle?.MatrixInfo == null) return;
			if (primaryEscapeShuttle?.MatrixInfo.MatrixMove == null) return;
			if (primaryEscapeShuttle?.MatrixInfo.MatrixMove.InitialFacing == null) return;
		}

		var orientation = primaryEscapeShuttle.MatrixInfo.MatrixMove.InitialFacing;
		float width;

		if (orientation== Orientation.Up || orientation == Orientation.Down)
		{
			width = PrimaryEscapeShuttle.MatrixInfo.Bounds.size.x;
		}
		else
		{
			width = PrimaryEscapeShuttle.MatrixInfo.Bounds.size.y;
		}

		Vector3 newPos;

		switch (LandingZoneManager.Instance.centcomDocking.orientation)
		{
			case OrientationEnum.Right:
				newPos = new Vector3(LandingZoneManager.Instance.centcomDockingPos.x + Mathf.Ceil(width/2f),LandingZoneManager.Instance.centcomDockingPos.y, 0);
				break;
			case OrientationEnum.Up:
				newPos = new Vector3(LandingZoneManager.Instance.centcomDockingPos.x ,LandingZoneManager.Instance.centcomDockingPos.y + Mathf.Ceil(width/2f), 0);
				break;
			case OrientationEnum.Left:
				newPos = new Vector3(LandingZoneManager.Instance.centcomDockingPos.x - Mathf.Ceil(width/2f),LandingZoneManager.Instance.centcomDockingPos.y, 0);
				break;
			default:
				newPos = new Vector3(LandingZoneManager.Instance.centcomDockingPos.x ,LandingZoneManager.Instance.centcomDockingPos.y - Mathf.Ceil(width/2f), 0);
				break;
		}

		PrimaryEscapeShuttle.MatrixInfo.MatrixMove.ChangeFacingDirection(Orientation.FromEnum(PrimaryEscapeShuttle.orientationForDockingAtCentcom));
		PrimaryEscapeShuttle.MatrixInfo.MatrixMove.SetPosition(newPos);
		primaryEscapeShuttle.InitDestination(newPos);

		bool beenToStation = false;

		PrimaryEscapeShuttle.OnShuttleUpdate?.AddListener( status =>
		{
			//status display ETA tracking
			if ( status == EscapeShuttleStatus.OnRouteStation )
			{
				PrimaryEscapeShuttle.OnTimerUpdate.AddListener( TrackETA );
			} else
			{
				PrimaryEscapeShuttle.OnTimerUpdate.RemoveListener( TrackETA );
				CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, string.Empty);
			}

			if ( status == EscapeShuttleStatus.DockedCentcom && beenToStation )
			{
				Logger.Log("Shuttle arrived at Centcom", Category.Round);
				Chat.AddSystemMsgToChat($"<color=white>Escape shuttle has docked at Centcomm! Round will restart in {TimeSpan.FromSeconds(RoundEndTime).Minutes} minute.</color>", MatrixManager.MainStationMatrix);
				StartCoroutine(WaitForRoundEnd());
			}

			IEnumerator WaitForRoundEnd()
			{
				Logger.Log($"Shuttle docked to Centcom, Round will end in {TimeSpan.FromSeconds(RoundEndTime).Minutes} minute", Category.Round);
				yield return WaitFor.Seconds(1f);
				EndRound();
			}

			if (status == EscapeShuttleStatus.DockedStation)
			{
				beenToStation = true;
				SoundManager.PlayNetworked("ShuttleDocked");
				Chat.AddSystemMsgToChat($"<color=white>Escape shuttle has arrived! Crew has {TimeSpan.FromSeconds(ShuttleDepartTime).Minutes} minutes to get on it.</color>", MatrixManager.MainStationMatrix);
				//should be changed to manual send later
				StartCoroutine( SendEscapeShuttle( ShuttleDepartTime ) );
			}
		} );
	}

	private void TrackETA(int eta)
	{
		CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, FormatTime( eta, "STATION\nETA: " ) );
	}

	public static string FormatTime( int timerSeconds, string prefix = "ETA: " )
	{
		if ( timerSeconds < 0 )
		{
			return string.Empty;
		}

		return prefix+TimeSpan.FromSeconds( timerSeconds ).ToString( "mm\\:ss" );
	}

	private IEnumerator SendEscapeShuttle( int seconds )
	{
		//departure countdown
		for ( int i = seconds; i >= 0; i-- )
		{
			CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, FormatTime(i, "Depart\nETA: ") );
			yield return WaitFor.Seconds(1);
		}

		PrimaryEscapeShuttle.SendShuttle();

		//centcom round end countdown
		int timeToCentcom = (seconds * 2 - 2);
		for ( int i = timeToCentcom - 1; i >= 0; i-- )
		{
			CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, FormatTime(i, "CENTCOM\nETA: ") );
			yield return WaitFor.Seconds(1);
		}

		CentComm.UpdateStatusDisplay( StatusDisplayChannel.EscapeShuttle, string.Empty);
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
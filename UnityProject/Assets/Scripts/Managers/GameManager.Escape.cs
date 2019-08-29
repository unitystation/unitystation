
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

		bool beenToStation = false;
		
		PrimaryEscapeShuttle.OnShuttleUpdate?.AddListener( status =>
		{
			if ( status == ShuttleStatus.DockedCentcom && beenToStation )
			{
				RoundEnd();
				Logger.Log( "Shuttle arrived to Centcom, Round should end here", Category.Round );
			}

			if (status == ShuttleStatus.DockedStation)
			{
				beenToStation = true;
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
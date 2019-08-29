using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Non-unique escape shuttle. Serverside methods only
/// </summary>
public class EscapeShuttle : MonoBehaviour
{
	public MatrixInfo MatrixInfo => mm.MatrixInfo;
	private MatrixMove mm;

	public ShuttleStatusEvent OnShuttleUpdate = new ShuttleStatusEvent();
	public ShuttleTimerEvent OnTimerUpdate = new ShuttleTimerEvent();

	public Destination CentcomDest = new Destination {Orientation = Orientation.Right, Position = new Vector2( 150, 6 ), ApproachReversed = false};
	public Destination StationDest = new Destination {Orientation = Orientation.Right, Position = new Vector2( 49, 6 ), ApproachReversed = true};
	private Destination currentDestination;

	public float DistanceToDestination => Vector2.Distance( mm.State.Position, currentDestination.Position );

	/// <summary>
	/// Seconds for shuttle call
	/// </summary>
	public int InitialTimerSeconds
	{
		get => initialTimerSeconds;
		set => initialTimerSeconds = value;
	}
	[Range( 0, 2000 )] [SerializeField] private int initialTimerSeconds = 120;

	/// <summary>
	/// How many seconds should be left before arrival when recall should be blocked
	/// </summary>
	public int TooLateToRecallSeconds
	{
		get => tooLateToRecallSeconds;
		set => tooLateToRecallSeconds = value;
	}
	[Range( 0, 1000 )] [SerializeField] private int tooLateToRecallSeconds = 60;

	/// <summary>
	/// Current "flight" time
	/// </summary>
	public int CurrentTimerSeconds
	{
		get => currentTimerSeconds;
		private set
		{
			currentTimerSeconds = value;
			OnTimerUpdate.Invoke( currentTimerSeconds );
		}
	}
	private int currentTimerSeconds = 0;

	/// <summary>
	/// Assign initial status via Editor
	/// </summary>
	public ShuttleStatus Status
	{
		get => internalStatus;
		set
		{
			internalStatus = value;
			OnShuttleUpdate?.Invoke( internalStatus );
			Logger.LogTrace( gameObject.name + " EscapeShuttle status changed to " + internalStatus );
		}
	}
	[SerializeField] private ShuttleStatus internalStatus = ShuttleStatus.DockedCentcom;

	/// <summary>
	/// used for convenient control with our coroutine extensions
	/// </summary>
	private Coroutine timerHandle;

	private void Awake()
	{
		mm = GetComponent<MatrixMove>();

		OnShuttleUpdate.AddListener( RemovePark );
	}

	private void Update()
	{
		if ( !CustomNetworkManager.Instance._isServer || currentDestination == Destination.Invalid )
		{
			return;
		}

		//arrived to destination
		if ( mm.State.IsMoving )
		{

			if ( DistanceToDestination < 2 )
			{
				mm.SetPosition( currentDestination.Position );
				mm.StopMovement();

				//centcom docked state is set manually instead, as we should usually pretend that flight is longer than it is
				if ( Status == ShuttleStatus.OnRouteStation )
				{
					Status = ShuttleStatus.DockedStation;
				}
			}
			else if ( DistanceToDestination < 25 && currentDestination.ApproachReversed )
			{
				TryPark();
			}
		}
	}

	//sorry, not really clean, robust or universal
	#region parking

	private bool parkingMode = false;
	private bool isReverse = false;

	private void TryPark()
	{
		//slowing down
		if ( !parkingMode )
		{
			parkingMode = true;
			mm.SetSpeed( 2 );
		}

		if ( !isReverse )
		{
			isReverse = true;
			mm.ChangeDir( mm.State.Direction.Rotate( 2 ) );
		}
	}

	private void RemovePark( ShuttleStatus unused )
	{
		if ( parkingMode )
		{
			mm.ChangeDir( mm.State.Direction.Rotate( 2 ) );
			isReverse = false;
		}

		parkingMode = false;
	}

	#endregion

	/// <summary>
	/// Calls the shuttle from afar.
	/// </summary>
	public bool CallShuttle(out string callResult, int seconds = 0)
	{
		if ( Status != ShuttleStatus.DockedCentcom )
		{
			callResult = "Can't call shuttle: not docked at Centcom!";
			return false;
		}

		//don't change InitialTimerSeconds if they weren't passed over
		if ( seconds > 0 )
		{
			InitialTimerSeconds = seconds;
		}

		CurrentTimerSeconds = InitialTimerSeconds;
		mm.StopMovement();
		Status = ShuttleStatus.OnRouteStation;

		//start ticking timer
		this.TryStopCoroutine( ref timerHandle );
		this.StartCoroutine( TickTimer(), ref timerHandle );

		//adding a temporary listener:
		//start actually moving ship if it's 30s before arrival and it hasn't been recalled...
		void Action( int time )
		{
			if ( time <= 30 )
			{
				MoveToStation();
				OnTimerUpdate.RemoveListener( Action ); //self-remove after firing once
			}
		}

		OnTimerUpdate.AddListener( Action );

		//...otherwise above thing gets aborted and never executes
		OnShuttleUpdate.AddListener( newStatus =>
		{
			if ( newStatus != ShuttleStatus.OnRouteStation )
			{
				OnTimerUpdate.RemoveListener( Action );
			}
		} );

		callResult = "Shuttle has been called.";
		return true;
	}

	public bool RecallShuttle(out string callResult)
	{
		if ( Status != ShuttleStatus.OnRouteStation
		  || CurrentTimerSeconds < TooLateToRecallSeconds )
		{
			callResult = "Can't recall shuttle: not on route to Station or too late to recall!";
			return false;
		}

		this.TryStopCoroutine( ref timerHandle );
		this.StartCoroutine( TickTimer( true ), ref timerHandle );

		void Action( int time )
		{
			if ( time >= InitialTimerSeconds )
			{
				Status = ShuttleStatus.DockedCentcom;
				OnTimerUpdate.RemoveListener( Action ); //self-remove after firing once
			}
		}

		OnTimerUpdate.AddListener( Action );
		OnShuttleUpdate.AddListener( newStatus =>
		{
			if ( newStatus != ShuttleStatus.OnRouteCentcom )
			{
				OnTimerUpdate.RemoveListener( Action );
			}
		} );

		mm.StopMovement();
		Status = ShuttleStatus.OnRouteCentcom;

		MoveToCentcom();

		callResult = "Shuttle has been recalled.";
		return true;
	}

	/// <summary>
	/// Should send arrived shuttle to Centcom, with Heads' blessing or otherwise
	/// But! it sends shuttle into abyss with increasing speed for now
	/// </summary>
	public void SendShuttle()
	{
		Status = ShuttleStatus.OnRouteCentcom;

		currentDestination = Destination.Invalid;
		mm.SetSpeed( 1 );
		mm.StartMovement();
		mm.maxSpeed = float.MaxValue; //hehe

		StartCoroutine(LosingMyFavouriteGame());
		IEnumerator LosingMyFavouriteGame()
		{
			while ( Status == ShuttleStatus.OnRouteCentcom )
			{
				yield return WaitFor.Seconds( 1.5f );
				mm.SetSpeed( mm.State.Speed * 1.5f );
			}
		}
	}


	private IEnumerator TickTimer( bool inverse = false )
	{
		while ( inverse ? (CurrentTimerSeconds < InitialTimerSeconds) : (CurrentTimerSeconds > 0) )
		{
			if ( inverse )
			{
				CurrentTimerSeconds += 1;
			} else
			{
				CurrentTimerSeconds -= 1;
			}

			yield return WaitFor.Seconds( 1 );
		}
	}

	/// <summary>
	/// Send Shuttle to the station immediately.
	/// Server only.
	/// </summary>
	public void MoveToStation()
	{
		mm.SetSpeed( 25 );
		MoveTo( StationDest );
	}

	/// <summary>
	/// Send shuttle to centcom immediately.
	/// Server only.
	/// </summary>
	public void MoveToCentcom()
	{
		mm.SetSpeed( 25 );
		MoveTo( CentcomDest );
	}

	private void MoveTo( Destination dest )
	{
		currentDestination = dest;
		mm.AutopilotTo( currentDestination.Position );
	}
}

public class ShuttleStatusEvent : UnityEvent<ShuttleStatus> { }

public class ShuttleTimerEvent : UnityEvent<int> { }
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

/// <summary>
/// Non-unique escape shuttle. Serverside methods only
/// </summary>
public class EscapeShuttle : NetworkBehaviour
{
	// Indicates at which moment (in remaining seconds) the shuttle should really start moving
	private const int StartMovingAtCount = 30;

	public MatrixInfo MatrixInfo => mm.MatrixInfo;
	private MatrixMove mm;

	public ShuttleStatusEvent OnShuttleUpdate = new ShuttleStatusEvent();
	public ShuttleTimerEvent OnTimerUpdate = new ShuttleTimerEvent();


	void Start()
	{
		if (OrientationRight == true)
		{
			CentcomDest = new Destination { Orientation = Orientation.Right, Position = DockingLocationCentcom, ApproachReversed = false };
			StationDest = new Destination { Orientation = Orientation.Right, Position = DockingLocationStation, ApproachReversed = true };
		}
		else if (OrientationUp == true)
		{
			CentcomDest = new Destination { Orientation = Orientation.Up, Position = DockingLocationCentcom, ApproachReversed = false };
			StationDest = new Destination { Orientation = Orientation.Up, Position = DockingLocationStation, ApproachReversed = true };
		}
		else
		{
			CentcomDest = new Destination { Orientation = Orientation.Right, Position = new Vector2(150, 6), ApproachReversed = false };
			StationDest = new Destination { Orientation = Orientation.Right, Position = new Vector2(49, 6), ApproachReversed = true };
		}
	}

	public bool OrientationRight;
	public bool OrientationUp;
	public Vector2 DockingLocationStation;
	public Destination CentcomDest;
	public Vector2 DockingLocationCentcom;
	public Destination StationDest;

	private Destination currentDestination;

	[Tooltip("If escape shuttle movement is blocked for longer than this amount of time, will end the round" +
	         " with the escape impossible ending.")]
	[SerializeField]
	private int escapeBlockTimeLimit = 10;

	///tracks how long escape shuttle movement has been blocked to see if ending should be triggered.
	private float escapeBlockedTime;
	private bool isBlocked;

	// Indicate if the shuttle really started moving toward station (It really starts moving in the StartMovingAtCount remaining seconds)
	private bool startedMovingToStation;

	public float DistanceToDestination => Vector2.Distance( mm.ServerState.Position, currentDestination.Position );

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

	/// <summary>
	/// True iff this shuttle has at least one functional thruster.
	/// </summary>
	public bool HasWorkingThrusters => thrusters != null && thrusters.Count > 0;

	[SerializeField] private ShuttleStatus internalStatus = ShuttleStatus.DockedCentcom;

	/// <summary>
	/// used for convenient control with our coroutine extensions
	/// </summary>
	private Coroutine timerHandle;

	/// <summary>
	/// tracks the thrusters we have so we can check for game over when it's immobilized.
	/// Note it's not currently possible to construct thrusters. This is only stored server side.
	/// Thrusters are removed from this when destroyed
	/// </summary>
	private List<ShipThruster> thrusters = new List<ShipThruster>();

	private void Awake()
	{
		mm = GetComponent<MatrixMove>();

		OnShuttleUpdate.AddListener( RemovePark );

		//note:
		thrusters = GetComponentsInChildren<ShipThruster>().ToList();
		//subscribe to their integrity events so we can update when they are destroyed
		foreach (var thruster in thrusters)
		{
			var integrity = thruster.GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(OnWillDestroyThruster);
		}

	}

	//called when each thruster is destroyed
	private void OnWillDestroyThruster(DestructionInfo destruction)
	{
		if (!CustomNetworkManager.IsServer) return;
		thrusters.Remove(destruction.Destroyed.GetComponent<ShipThruster>());

		if (thrusters.Count == 0)
		{
			ServerStartStrandedEnd();
		}
	}

	private void ServerStartStrandedEnd()
	{
		//game over! escape shuttle has no thrusters so it's not possible to reach centcomm.
		currentDestination = Destination.Invalid;
		RpcStrandedEnd();
		StartCoroutine(WaitForGameOver());
		GameManager.Instance.RespawnCurrentlyAllowed = false;
	}

	IEnumerator WaitForGameOver()
	{
		//note: used to wait for 25 seconds, now less because
		//we disabled the zoom out
		yield return WaitFor.Seconds(15f);
		// Trigger end of round
		GameManager.Instance.EndRound();
	}

	[ClientRpc]
	private void RpcStrandedEnd()
	{
		UIManager.Instance.PlayStrandedAnimation();
	}

	private void Update()
	{
		if ( !CustomNetworkManager.Instance._isServer || currentDestination == Destination.Invalid )
		{
			return;
		}

		//arrived to destination
		if ( mm.ServerState.IsMoving )
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

		//check if we're trying to move but are unable to
		if (!isBlocked)
		{
			if (Status != ShuttleStatus.DockedCentcom && Status != ShuttleStatus.DockedStation)
			{
				if ((!mm.ServerState.IsMoving || mm.ServerState.Speed < 1f) && startedMovingToStation)
				{
					Logger.LogTrace("Escape shuttle is blocked.", Category.Matrix);
					isBlocked = true;
					escapeBlockedTime = 0f;
				}
			}
		}
		else
		{
			//currently blocked, check if we are unblocked
			if (Status == ShuttleStatus.DockedCentcom || Status == ShuttleStatus.DockedStation ||
			    (mm.ServerState.IsMoving && mm.ServerState.Speed >= 1f))
			{
				Logger.LogTrace("Escape shuttle is unblocked.", Category.Matrix);
				isBlocked = false;
				escapeBlockedTime = 0f;
			}
			else
			{
				//continue being blocked
				escapeBlockedTime += Time.deltaTime;
				if (escapeBlockedTime > escapeBlockTimeLimit)
				{
					Logger.LogTraceFormat("Escape shuttle blocked for more than {0} seconds, stranded ending playing.", Category.Matrix, escapeBlockTimeLimit);
					//can't escape
					ServerStartStrandedEnd();
				}
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
			mm.ChangeFacingDirection(mm.ServerState.FacingDirection.Rotate(2));
			PlaySoundMessage.SendToAll("ShuttleDocked", Vector3.zero, 1f);
		}
	}

	private void RemovePark( ShuttleStatus unused )
	{
		if ( parkingMode )
		{
			mm.ChangeFlyingDirection(mm.ServerState.FacingDirection);
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
		startedMovingToStation = false;

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
		//start actually moving ship if it's StartMovingAtCount seconds before arrival and it hasn't been recalled...
		void Action( int time )
		{
			if ( time <= StartMovingAtCount)
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
		startedMovingToStation = false;

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
		mm.SetSpeed( 100f );
		mm.StartMovement();
		mm.MaxSpeed = 100f;
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
		startedMovingToStation = true;
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
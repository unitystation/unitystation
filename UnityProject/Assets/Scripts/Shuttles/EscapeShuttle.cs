using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Logs;
using Managers;
using Tilemaps.Behaviours.Layers;
using UnityEngine;
using UnityEngine.Events;
using Objects.Wallmounts;

public class EscapeShuttle : MonoBehaviour
{
	public MatrixInfo MatrixInfo => matrixMove.MatrixInfo;
	private MatrixMove matrixMove;

	private CentComm centComm;

	public ShuttleStatusEvent OnShuttleUpdate = new ShuttleStatusEvent();
	public ShuttleTimerEvent OnTimerUpdate = new ShuttleTimerEvent();
	public event Action OnShuttleCalled;

	/// <summary>
	/// Orientation for docking at station, eg Up if north to south.
	/// </summary>
	[Tooltip("Orientation for docking at station, eg Up if north to south.")]
	public OrientationEnum orientationForDocking = OrientationEnum.Up_By0;

	/// <summary>
	/// Orientation for docking at CentCom, eg Up if south to north.
	/// </summary>
	[Tooltip("Orientation for docking at CentCom, eg Up if south to north.")]
	public OrientationEnum orientationForDockingAtCentcom = OrientationEnum.Right_By270;

	//Coord set in inspector
	public Vector2 stationDockingLocation;
	public Vector2 stationTeleportLocation;

	public int reverseDockOffset = 50;

	/// <summary>
	/// How far to travel after teleport until it reaches centcom.
	/// </summary>
	public int centComDockingOffset = 1000;

	//Destination Stuff
	[HideInInspector]
	public Destination CentcomDest;
	private Destination StationDest;

	[HideInInspector]
	public Destination CentTeleportToCentDock;

	private Destination currentDestination;

	[Tooltip("If escape shuttle movement is blocked for longer than this amount of time, will end the round" +
	         " with the escape impossible ending.")]
	[SerializeField]
	private int escapeBlockTimeLimit = 10;

	///tracks how long escape shuttle movement has been blocked to see if ending should be triggered.
	private float escapeBlockedTime;
	private bool isBlocked;

	// Checks if shuttle has docked at station
	private bool HasShuttleDockedToStation = false;

	// Indicate if the shuttle really started moving toward station (It really starts moving in the StartMovingAtCount remaining seconds)
	private bool startedMovingToStation;

	public float DistanceToDestination => Vector2.Distance( matrixMove.ServerState.Position, currentDestination.Position );

	/// <summary>
	/// used for convenient control with our coroutine extensions
	/// </summary>
	private Coroutine timerHandle;

	/// <summary>
	/// Seconds for shuttle call, affected by alert level
	/// </summary>
	public int InitialTimerSeconds
	{
		get => initialTimerSeconds;
		set => initialTimerSeconds = value;
	}
	[Range( 0, 2000 )] [SerializeField] private int initialTimerSeconds = 120;

	private int initialTimerSecondsCache;

	/// <summary>
	/// How many seconds should be left before arrival when recall should be blocked, affected by alert level
	/// </summary>
	[Range( 0, 1000 )] private int TooLateToRecallSeconds = 60;

	/// <summary>
	/// Current "flight" time
	/// </summary>
	public int CurrentTimerSeconds { get; private set; }

	/// <summary>
	/// Assign initial status via Editor
	/// </summary>
	public EscapeShuttleStatus Status
	{
		get => internalStatus;
		set
		{
			internalStatus = value;
			OnShuttleUpdate.Invoke(internalStatus);
			GameManager.Instance.OnShuttleUpdate(internalStatus);
			Loggy.LogTrace( gameObject.name + " EscapeShuttle status changed to " + internalStatus );
		}
	}

	/// <summary>
	/// True iff this shuttle has at least one functional thruster.
	/// </summary>
	public bool HasWorkingThrusters => thrusters != null && thrusters.Count > 0;

	/// <summary>
	/// tracks the thrusters we have so we can check for game over when it's immobilized.
	/// Note it's not currently possible to construct thrusters. This is only stored server side.
	/// Thrusters are removed from this when destroyed
	/// </summary>
	private List<ShipThruster> thrusters = new List<ShipThruster>();

	[SerializeField] private EscapeShuttleStatus internalStatus = EscapeShuttleStatus.DockedCentcom;

	private Vector3 centComTeleportPosOffset = Vector3.zero;

	[HideInInspector]
	public bool blockCall;

	[HideInInspector]
	public bool blockRecall;

	public bool hostileEnvironment => hostileEnvironmentCounter >= 1;

	private int hostileEnvironmentCounter = 0;

	private NetworkedMatrix networkedMatrix;

	private bool parkingMode = false;
	private bool isReverse = false;

	private void Start()
	{
		switch (orientationForDocking)
		{
			case OrientationEnum.Right_By270:
				CentcomDest = new Destination { Orientation = Orientation.Right, Position = stationTeleportLocation};
				StationDest = new Destination { Orientation = Orientation.Right, Position = stationDockingLocation};
				break;
			case OrientationEnum.Up_By0:
				CentcomDest = new Destination { Orientation = Orientation.Up, Position = stationTeleportLocation};
				StationDest = new Destination { Orientation = Orientation.Up, Position = stationDockingLocation};
				break;
			case OrientationEnum.Left_By90:
				CentcomDest = new Destination { Orientation = Orientation.Left, Position = stationTeleportLocation};
				StationDest = new Destination { Orientation = Orientation.Left, Position = stationDockingLocation};
				break;
			case OrientationEnum.Down_By180:
				CentcomDest = new Destination { Orientation = Orientation.Down, Position = stationTeleportLocation};
				StationDest = new Destination { Orientation = Orientation.Down, Position = stationDockingLocation};
				break;
		}

		centComm = GameManager.Instance.GetComponent<CentComm>();

		initialTimerSecondsCache = initialTimerSeconds;
	}

	public void InitDestination(Vector3 newPos)
	{
		Orientation orientation = Orientation.Right;

		switch (orientationForDockingAtCentcom)
		{
			case OrientationEnum.Up_By0:
				centComTeleportPosOffset += new Vector3(0, -centComDockingOffset, 0);
				orientation = Orientation.Up;
				break;
			case OrientationEnum.Down_By180:
				centComTeleportPosOffset += new Vector3(0, centComDockingOffset, 0);
				orientation = Orientation.Down;
				break;
			case OrientationEnum.Left_By90:
				centComTeleportPosOffset += new Vector3(centComDockingOffset, 0, 0);
				orientation = Orientation.Left;
				break;
			default:
				centComTeleportPosOffset += new Vector3(-centComDockingOffset, 0, 0);
				orientation = Orientation.Right;
				break;
		}

		CentTeleportToCentDock = new Destination { Orientation = orientation, Position = newPos};
	}

	private void Awake()
	{
		matrixMove = GetComponent<MatrixMove>();
		networkedMatrix = GetComponent<NetworkedMatrix>();

		thrusters = GetComponentsInChildren<ShipThruster>().ToList();
		foreach (var thruster in thrusters)
		{
			var integrity = thruster.GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(OnWillDestroyThruster);
		}
	}

	private void OnEnable()
	{
		if(CustomNetworkManager.IsServer == false) return;

		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		StopAllCoroutines();

		if(CustomNetworkManager.IsServer == false) return;

		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
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
		networkedMatrix.MatrixSync.RpcStrandedEnd();
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

	private void UpdateMe()
	{
		if (currentDestination == Destination.Invalid ) return;

		//arrived to destination
		if ( matrixMove.ServerState.IsMoving )
		{

			if (DistanceToDestination < 200)
			{
				matrixMove.SetSpeed(80);
			}

			if ( DistanceToDestination < 2 )
			{
				matrixMove.SetPosition( currentDestination.Position );
				matrixMove.StopMovement();

				//centcom docked state is set manually instead, as we should usually pretend that flight is longer than it is
				if ( Status == EscapeShuttleStatus.OnRouteStation )
				{
					Status = EscapeShuttleStatus.DockedStation;
					HasShuttleDockedToStation = true;
				}
				else if(Status == EscapeShuttleStatus.OnRouteToStationTeleport)
				{
					Status = EscapeShuttleStatus.OnRouteToCentCom;

					TeleportToCentTeleport();
				}
				else if(Status == EscapeShuttleStatus.OnRouteToCentCom)
				{
					Status = EscapeShuttleStatus.DockedCentcom;
					if (Status == EscapeShuttleStatus.DockedCentcom && HasShuttleDockedToStation == true)
					{
						SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.HyperSpaceEnd, transform.position, sourceObj: gameObject);
					}
				}
			}

			else if ( DistanceToDestination < reverseDockOffset && Status == EscapeShuttleStatus.OnRouteStation)
			{
				TryPark();
			}
		}

		//check if we're trying to move but are unable to
		if (!isBlocked)
		{
			if (Status != EscapeShuttleStatus.DockedCentcom && Status != EscapeShuttleStatus.DockedStation)
			{
				if ((!matrixMove.ServerState.IsMoving || matrixMove.ServerState.Speed < 1f) && startedMovingToStation)
				{
					Loggy.LogTrace("Escape shuttle is blocked.", Category.Shuttles);
					isBlocked = true;
					escapeBlockedTime = 0f;
				}
			}
		}
		else
		{
			//currently blocked, check if we are unblocked
			if (Status == EscapeShuttleStatus.DockedCentcom || Status == EscapeShuttleStatus.DockedStation ||
			    (matrixMove.ServerState.IsMoving && matrixMove.ServerState.Speed >= 1f))
			{
				Loggy.LogTrace("Escape shuttle is unblocked.", Category.Shuttles);
				isBlocked = false;
				escapeBlockedTime = 0f;
			}
			else
			{
				//continue being blocked
				escapeBlockedTime += Time.deltaTime;
				if (escapeBlockedTime > escapeBlockTimeLimit)
				{
					Loggy.LogTraceFormat("Escape shuttle blocked for more than {0} seconds, stranded ending playing.", Category.Shuttles, escapeBlockTimeLimit);
					//can't escape
					ServerStartStrandedEnd();
				}
			}
		}
	}

	//sorry, not really clean, robust or universal
	#region parking

	private void TryPark()
	{
		//slowing down
		if ( !parkingMode )
		{
			parkingMode = true;
			matrixMove.SetSpeed( 2 );
		}

		if ( !isReverse )
		{
			isReverse = true;
			matrixMove.ChangeFacingDirection(matrixMove.ServerState.FacingDirection.Rotate(2));
			/*
			if (Status == ShuttleStatus.DockedStation)
			{
				PlaySoundMessage.SendToAll("ShuttleDocked", Vector3.zero, 1f);
			}
			else {}
			*/
			HasShuttleDockedToStation = true;
		}
	}

	private void RemovePark( ShuttleStatus unused )
	{
		if ( parkingMode )
		{
			matrixMove.ChangeFlyingDirection(matrixMove.ServerState.FacingDirection);
			isReverse = false;
		}

		parkingMode = false;
	}

	#endregion

	#region Moving To Station

	/// <summary>
	/// Calls the shuttle from afar.
	/// </summary>
	public bool CallShuttle(out string callResult, int seconds = 0, bool bypassLimits = false)
	{
		if (blockCall && !bypassLimits)
		{
			callResult = "The emergency shuttle cannot be called at this time.";
			return false;
		}

		if ( Status != EscapeShuttleStatus.DockedCentcom )
		{
			callResult = "Can't call shuttle: not docked at Centcom!";
			return false;
		}

		startedMovingToStation = false;

		var Alert = centComm.CurrentAlertLevel;

		//Changes EscapeShuttle time depending on Alert Level

		if (Alert == CentComm.AlertLevel.Green)
		{
			//Double the Time
			InitialTimerSeconds = initialTimerSecondsCache * 2;
		}
		else if (Alert == CentComm.AlertLevel.Blue)
        {
			//Default values set in inspector
			InitialTimerSeconds = initialTimerSecondsCache;
        }
		else if (Alert == CentComm.AlertLevel.Red || Alert == CentComm.AlertLevel.Delta)
		{
			//Half the Time
			InitialTimerSeconds = initialTimerSecondsCache / 2;
		}

		TooLateToRecallSeconds = InitialTimerSeconds / 2;


		//don't change InitialTimerSeconds if they weren't passed over
		if ( seconds > 0 )
		{
			InitialTimerSeconds = seconds;
		}

		CurrentTimerSeconds = InitialTimerSeconds;
		matrixMove.StopMovement();
		Status = EscapeShuttleStatus.OnRouteStation;

		//start ticking timer
		this.TryStopCoroutine( ref timerHandle );
		this.StartCoroutine( TickTimer(), ref timerHandle );
		OnShuttleCalled?.Invoke();

		callResult = "Shuttle has been called.";
		return true;
	}

	#endregion

	#region Recall

	public bool RecallShuttle(out string callResult, bool ignoreTooLateToRecall = false)
	{
		if (blockRecall && !ignoreTooLateToRecall)
		{
			callResult = "The emergency shuttle cannot be recalled at this time.";
			return false;
		}

		if ( Status != EscapeShuttleStatus.OnRouteStation || (!ignoreTooLateToRecall && CurrentTimerSeconds < TooLateToRecallSeconds) )
		{
			callResult = "Can't recall shuttle: not on route to Station or too late to recall!";
			return false;
		}

		startedMovingToStation = false;

		this.TryStopCoroutine( ref timerHandle );
		this.StartCoroutine( TickTimer(false), ref timerHandle );

		matrixMove.StopMovement();
		Status = EscapeShuttleStatus.OnRouteToCentCom;

		HasShuttleDockedToStation = false;

		matrixMove.SetPosition( CentTeleportToCentDock.Position + centComTeleportPosOffset);
		matrixMove.SetSpeed( 90 );
		MoveTo(CentTeleportToCentDock);

		callResult = "Shuttle has been recalled.";
		return true;
	}


	#endregion

	#region Moving To CentCom

	public void SendShuttle()
	{
		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.HyperSpaceBegin, transform.position, sourceObj: gameObject);

		StartCoroutine(WaitForShuttleLaunch());
	}

	IEnumerator WaitForShuttleLaunch()
	{
		yield return WaitFor.Seconds(7f);

		SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.HyperSpaceProgress, transform.position, sourceObj: gameObject);

		Status = EscapeShuttleStatus.OnRouteToStationTeleport;

		matrixMove.SetSpeed(100f);
		matrixMove.StartMovement();
		matrixMove.MaxSpeed = 100f;
		MoveTo( CentcomDest );
	}

	public void TeleportToCentTeleport()
	{
		matrixMove.StopMovement();
		matrixMove.SetPosition(CentTeleportToCentDock.Position + centComTeleportPosOffset);
		MoveTo(CentTeleportToCentDock);
	}

	#endregion

	private IEnumerator TickTimer(bool headingToStation = true)
	{
		while (true)
		{
			if (headingToStation)
			{
				AddToTime(-1);
				//Time = Distance/Speed
				if (startedMovingToStation == false && CurrentTimerSeconds <= Vector2.Distance(stationTeleportLocation, stationDockingLocation) / matrixMove.MaxSpeed + 10f)
				{
					startedMovingToStation = true;
					matrixMove.SetPosition(stationTeleportLocation);
					matrixMove.SetSpeed(matrixMove.MaxSpeed);
					MoveTo(StationDest);
				}
				if (CurrentTimerSeconds <= 0)
				{
					centComm.UpdateStatusDisplay(StatusDisplayChannel.CachedChannel, null);
					yield break;
				}
			}
			else
			{
				AddToTime(1);
				if (CurrentTimerSeconds >= InitialTimerSeconds)
				{
					Status = EscapeShuttleStatus.DockedCentcom;
					centComm.UpdateStatusDisplay(StatusDisplayChannel.CachedChannel, null);
					yield break;
				}
			}
			yield return WaitFor.Seconds(1);
		}
	}

	private void AddToTime(int value)
	{
		CurrentTimerSeconds += value;
		OnTimerUpdate.Invoke(CurrentTimerSeconds);
		centComm.UpdateStatusDisplay(StatusDisplayChannel.EscapeShuttle, StatusDisplay.FormatTime( CurrentTimerSeconds, "STATION\nETA: "));
	}

	private void MoveTo( Destination dest )
	{
		currentDestination = dest;
		matrixMove.AutopilotTo( currentDestination.Position );
	}

	public void SetHostileEnvironment(bool activateHostileEnviro)
	{
		if (activateHostileEnviro)
		{
			hostileEnvironmentCounter += 1;
			return;
		}

		if(hostileEnvironmentCounter > 1)
		{
			hostileEnvironmentCounter -= 1;
			return;
		}

		hostileEnvironmentCounter = 0;

		if(Status != EscapeShuttleStatus.DockedStation) return;

		Chat.AddSystemMsgToChat($"<color=white>Hostile Environment has been removed! Crew has {TimeSpan.FromSeconds(GameManager.Instance.ShuttleDepartTime).Minutes} minutes to get on it.</color>", MatrixManager.MainStationMatrix, LanguageManager.Common);
		GameManager.Instance.ForceSendEscapeShuttleFromStation(GameManager.Instance.ShuttleDepartTime);
	}
}

public enum EscapeShuttleStatus
{
	OnRouteStation = 0,
	DockedStation = 1,
	OnRouteToStationTeleport = 2,
	OnRouteToCentCom = 3,
	DockedCentcom = 4
}

public class ShuttleStatusEvent : UnityEvent<EscapeShuttleStatus> { }

public class ShuttleTimerEvent : UnityEvent<int> { }

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

public class EscapeShuttle : AutopilotShipMachine
{
	public MatrixInfo MatrixInfo
	{
		get
		{
			return matrixMove.NetworkedMatrixMove.MetaTileMap.matrix.MatrixInfo;
		}
	}

	public GuidanceBuoy StationStartBuoy;

	public GuidanceBuoy TargetDestinationBuoy => centComm.CentCommGuidanceBuoy;

	private MatrixMove matrixMove;

	private CentComm centComm;

	public ShuttleStatusEvent OnShuttleUpdate = new ShuttleStatusEvent();
	public ShuttleTimerEvent OnTimerUpdate = new ShuttleTimerEvent();
	public event Action OnShuttleCalled;


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

	private bool Initialised = false;

	public OrientationEnum CentralCommandOverrideDirection = OrientationEnum.Default;


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

	public int GiveUpTime { get; private set; } = 200;

	/// <summary>
	/// Current "flight" time
	/// </summary>
	public int UnderflowIndex { get; private set; }

	public List<string> UnderflowFunnies = new List<string>()
	{
		"FISH",
		"25",
		"222",
		"1",
		"2+2",
		"=",
		"4",
		".-.",
		"77",
		"52",
		"3.1459",
		"UH",
		"PI",
		"69",
		"-93",
		"123",
		"ABC",
		"123",
		"BABY",
		"U",
		"&",
		"ME",
		"<3",
		"ASDFG",
		"OK",
		"HERE",
		"WE",
		"GO",
		"0"
	};


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


	[HideInInspector]
	public bool blockCall;

	[HideInInspector]
	public bool blockRecall;

	public bool hostileEnvironment => hostileEnvironmentCounter >= 1;

	private int hostileEnvironmentCounter = 0;

	private NetworkedMatrix networkedMatrix;

	private void Start()
	{
		base.Start();
		centComm = GameManager.Instance.GetComponent<CentComm>();
		initialTimerSecondsCache = initialTimerSeconds;
	}


	private void Awake()
	{
		matrixMove = GetComponentInParent<MatrixMove>();
		networkedMatrix = GetComponentInParent<NetworkedMatrix>();
		thrusters = GetComponentsInChildren<ShipThruster>().ToList();
		GameManager.Instance.SetEscapeShuttle(this);
		foreach (var thruster in thrusters)
		{
			var integrity = thruster.GetComponent<Integrity>();
			integrity.OnWillDestroyServer.AddListener(OnWillDestroyThruster);
		}



	}

	public void ReSet()
	{
		Initialised = false;
		Status = EscapeShuttleStatus.OnRouteToCentCom;
	}

	private void OnEnable()
	{
		if(CustomNetworkManager.IsServer == false) return;
		EventManager.AddHandler(Event.RoundEnded,  ReSet);
		UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
	}

	private void OnDisable()
	{
		StopAllCoroutines();

		if(CustomNetworkManager.IsServer == false) return;
		EventManager.RemoveHandler(Event.RoundEnded,  ReSet);
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

	public override void ReachedEndOfOutBuoyChain(GuidanceBuoy GuidanceBuoy)
	{
		if (StationStartBuoy == GuidanceBuoy)
		{
			DirectionOverride = CentralCommandOverrideDirection;
		}
	}


	public override void ReachedEndOfInBuoyChain(GuidanceBuoy GuidanceBuoy, GuidanceBuoy StartOfChain)
	{
		if (TargetDestinationBuoy == StartOfChain)
		{
			Status = EscapeShuttleStatus.DockedCentcom;
		}

		if (StationStartBuoy == StartOfChain)
		{
			SoundManager.PlayNetworkedAtPos(CommonSounds.Instance.HyperSpaceEnd, transform.position, sourceObj: gameObject);
			HasShuttleDockedToStation = true;
			Status = EscapeShuttleStatus.DockedStation;
		}
	}

	public override void UpdateMe()
	{
		base.UpdateMe();
		if (Initialised == false)
		{
			if (TargetDestinationBuoy != null)
			{
				Initialised = true;
				MoveDirectionIn = true;
				Status = EscapeShuttleStatus.OnRouteToCentCom;
				DirectionOverride = CentralCommandOverrideDirection;
				MoveToTargetBuoy(TargetDestinationBuoy);
			}
		}

		//check if we're trying to move but are unable to
		if (!isBlocked)
		{
			if (Status != EscapeShuttleStatus.DockedCentcom && Status != EscapeShuttleStatus.DockedStation)
			{
				if ((matrixMove.NetworkedMatrixMove.IsMoving == false) && startedMovingToStation)
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
			    (matrixMove.NetworkedMatrixMove.IsMoving))
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

		Status = EscapeShuttleStatus.OnRouteToCentCom;

		HasShuttleDockedToStation = false;

		matrixMove.NetworkedMatrixMove.AITravelSpeed = (90);
		MoveToTargetBuoy(TargetDestinationBuoy);

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

		matrixMove.NetworkedMatrixMove.AITravelSpeed = 100f;

		MoveToTargetBuoy( TargetDestinationBuoy );
	}

	#endregion

	private IEnumerator TickTimer(bool headingToStation = true)
	{
		UnderflowIndex = 0;


		while (true)
		{
			if (headingToStation)
			{
				if (Status == EscapeShuttleStatus.DockedStation)
				{
					centComm.UpdateStatusDisplay(StatusDisplayChannel.CachedChannel, null);
					break;
				}
				AddToTime(-1);
				//Time = Distance/Speed
				if (startedMovingToStation == false && CurrentTimerSeconds <= Vector2.Distance(matrixMove.NetworkedMatrixMove.TargetTransform.position, StationStartBuoy.transform.position) / matrixMove.NetworkedMatrixMove.AITravelSpeed + 10f)
				{
					startedMovingToStation = true;
					matrixMove.NetworkedMatrixMove.AITravelSpeed = 100;
					MoveToTargetBuoy( StationStartBuoy );
					Status = EscapeShuttleStatus.OnRouteStation;
				}
				if (CurrentTimerSeconds <= 0 && UnderflowFunnies.Count <= UnderflowIndex && GiveUpTime < 0)
				{
					Loggy.LogError("[GameManager.Escape/TickTimer()] - OH SHITTTT Shuttle got stuck on the Way to station AAAAAAAAAAAAAAAAAAAAAAAAAAAA emergency end round");
					GameManager.Instance.EndRound();
					centComm.UpdateStatusDisplay(StatusDisplayChannel.CachedChannel, null);
					yield break;
				}
			}
			else
			{
				if (Status == EscapeShuttleStatus.DockedCentcom)
				{
					centComm.UpdateStatusDisplay(StatusDisplayChannel.CachedChannel, null);
					break;
				}
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
		if (CurrentTimerSeconds > 0)
		{
			CurrentTimerSeconds += value;
			OnTimerUpdate.Invoke(CurrentTimerSeconds);
			centComm.UpdateStatusDisplay(StatusDisplayChannel.EscapeShuttle, StatusDisplay.FormatTime( CurrentTimerSeconds, "STATION\nETA: "));

		}
		else
		{
			if (value < 0)
			{
				if (UnderflowFunnies.Count <= UnderflowIndex)
				{
					GiveUpTime--;
				}
				else
				{
					centComm.UpdateStatusDisplay(StatusDisplayChannel.EscapeShuttle, "STATION\nETA: " + UnderflowFunnies[UnderflowIndex]);
					UnderflowIndex++;
				}

			}
		}
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

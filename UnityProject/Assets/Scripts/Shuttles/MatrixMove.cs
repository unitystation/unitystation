using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Behavior which allows an entire matrix to move and rotate (and be synced over the network).
/// This behavior must go on a gameobject that is the parent of the gameobject that has the actual Matrix component.
/// </summary>
public partial class MatrixMove : ManagedNetworkBehaviour, IPlayerControllable
{
	/// <summary>
	/// Set this to make sure collisions are correct for the MatrixMove
	/// For example, shuttles collide with floors but players don't
	/// </summary>
	public CollisionType matrixColliderType = CollisionType.Shuttle;

	/// <summary>
	/// If anything has a specific UI that needs to be set, it can change based off this var
	/// </summary>
	public UIType uiType = UIType.Nanotrasen;

	[Tooltip("Initial facing of the ship. Very important to set this correctly!")]
	[SerializeField]
	private OrientationEnum initialFacing;

	[Tooltip("Does it require fuel in order to fly?")]
	public bool RequiresFuel;

	/// <summary>
	/// Initial facing of the ship as mapped in the editor.
	/// </summary>
	public Orientation InitialFacing => Orientation.FromEnum(initialFacing);

	[Tooltip("Max flying speed of this matrix.")]
	[FormerlySerializedAs("maxSpeed")]
	public float MaxSpeed = 20f;

	[SyncVar][Tooltip("Whether safety is currently on, preventing collisions when sensors detect them.")]
	public bool SafetyProtocolsOn = true;


	[SyncVar(hook = nameof(SyncInitialPosition))]
	private Vector3 initialPosition;
	/// <summary>
	/// Initial position for offset calculation, set on start and never changed afterwards
	/// </summary>
	public Vector3Int InitialPosition => initialPosition.RoundToInt();

	[SyncVar(hook = nameof(SyncPivot))]
	private Vector3 pivot;
	/// <summary>
	/// local pivot point, set on start and never changed afterwards
	/// </summary>
	public Vector3Int Pivot => pivot.RoundToInt();

	/// <summary>
	/// All the various events that can be subscribed to on this matrix
	/// </summary>
	public readonly MatrixMoveEvents MatrixMoveEvents = new MatrixMoveEvents();

	/// <summary>
	/// Gets the rotation offset this matrix has from its initial mapped
	/// facing.
	/// </summary>
	public RotationOffset FacingOffsetFromInitial => ServerState.FacingOffsetFromInitial(this);

	/// <summary>
	/// If it is currently fuelled
	/// </summary>
	[NonSerialized]
	public bool IsFueled;

	private bool IsAutopilotEngaged => Target != TransformState.HiddenPos;

	private MatrixInfo matrixInfo;
	public MatrixInfo MatrixInfo => matrixInfo;
	private ShuttleFuelSystem shuttleFuelSystem;
	public ShuttleFuelSystem ShuttleFuelSystem => shuttleFuelSystem;

	private MatrixPositionFilter matrixPositionFilter = new MatrixPositionFilter();

	private Coroutine floatingSyncHandle;

	private List<ShipThruster> thrusters = new List<ShipThruster>();
	public bool HasWorkingThrusters => thrusters.Count > 0;

	private Vector3Int[] SensorPositions;
	private GameObject[] RotationSensors;
	private GameObject rotationSensorContainerObject;
	/// <summary>
	/// Tracks the rotation we are currently performing.
	/// Null when a rotation is not in progress.
	/// NOTE: This is not an offset from initialfacing, it's an offset from our current facing. So
	/// if we are turning 90 degrees right, this will be Right no matter what our initial conditions were.
	/// </summary>
	private RotationOffset? inProgressRotation;

	private readonly int rotTime = 90;
	[HideInInspector]
	private GUI_CoordReadout coordReadoutScript;

	private GUI_ShuttleControl shuttleControlGUI;
	private int moveCur = -1;
	private int moveLimit = -1;

	//tracks status of initializing this matrix move
	private bool clientStarted;
	private bool receivedInitialState;
	private bool pendingInitialRotation;
	/// <summary>
	/// Has this matrix move finished receiving its initial state from the server and rotating into its correct
	/// position?
	/// </summary>
	public bool Initialized => clientStarted && receivedInitialState;

	public override void OnStartClient()
	{
		StartCoroutine(WaitForMatrixManager());
	}

	public override void OnStartServer()
	{
		StartCoroutine(WaitForMatrixManager());
		base.OnStartServer();
	}

	IEnumerator WaitForMatrixManager()
	{
		while (!MatrixManager.IsInitialized)
		{
			yield return WaitFor.EndOfFrame;
		}

		yield return WaitFor.EndOfFrame;
		if (isServer)
		{
			InitServerState();

			MatrixMoveEvents.OnStartMovementServer.AddListener(() =>
			{
				if (floatingSyncHandle == null)
				{
					this.StartCoroutine(FloatingAwarenessSync(), ref floatingSyncHandle);
				}
			});
			MatrixMoveEvents.OnStopMovementServer.AddListener(() => this.TryStopCoroutine(ref floatingSyncHandle));

			NotifyPlayers();
		}
		else
		{
			SyncPivot(pivot, pivot);
			SyncInitialPosition(initialPosition, initialPosition);
			MatrixMoveNewPlayer.Send(netId);
			clientStarted = true;
		}
	}

	[Server]
	private void InitServerState()
	{
		serverState.FlyingDirection = InitialFacing;
		serverState.FacingDirection = InitialFacing;
		Logger.LogTraceFormat("{0} server initial facing / flying {1}", Category.Matrix, this, InitialFacing);

		Vector3Int initialPositionInt =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		SyncInitialPosition(initialPosition, initialPositionInt);

		var child = transform.GetChild( 0 );
		matrixInfo = MatrixManager.Get( child.gameObject );
		var childPosition = Vector3Int.CeilToInt(new Vector3(child.transform.position.x, child.transform.position.y, 0));
		SyncPivot(pivot, initialPosition - childPosition);

		Logger.LogTraceFormat("{0}: pivot={1} initialPos={2}", Category.Matrix, gameObject.name,
			pivot, initialPositionInt);
		serverState.Speed = 1f;
		serverState.Position = initialPosition;
		serverTargetState = serverState;
		clientState = serverState;

		RecheckThrusters();
		if ( thrusters.Count > 0 )
		{
			Logger.LogFormat( "{0}: Initializing {1} thrusters!", Category.Transform, matrixInfo.Matrix.name, thrusters.Count );
			foreach ( var thruster in thrusters )
			{
				var integrity = thruster.GetComponent<Integrity>();
				if ( integrity )
				{
					integrity.OnWillDestroyServer.AddListener( destructionInfo =>
					{
						if ( thrusters.Contains( thruster ) )
						{
							thrusters.Remove( thruster );
						}

						if ( thrusters.Count == 0 && IsMovingServer )
						{
							Logger.LogFormat( "All thrusters were destroyed! Stopping {0} soon!", Category.Transform, matrixInfo.Matrix.name );
							StartCoroutine( StopWithDelay(1f) );
						}
					}	);
				}
			}
		}

		if (SensorPositions == null)
		{
			CollisionSensor[] sensors = GetComponentsInChildren<CollisionSensor>();
			if (sensors.Length == 0)
			{
				SensorPositions = new Vector3Int[0];
				return;
			}

			SensorPositions = sensors.Select(sensor => Vector3Int.RoundToInt(sensor.transform.localPosition)).ToArray();

			Logger.Log($"Initialized sensors at {string.Join(",", SensorPositions)}," +
			           $" direction is {ServerState.FlyingDirection}", Category.Matrix);
		}

		if (RotationSensors == null)
		{
			RotationCollisionSensor[] sensors = GetComponentsInChildren<RotationCollisionSensor>();
			if (sensors.Length == 0)
			{
				RotationSensors = new GameObject[0];
				return;
			}

			if (rotationSensorContainerObject == null)
			{
				rotationSensorContainerObject = sensors[0].transform.parent.gameObject;
			}

			RotationSensors = sensors.Select(sensor => sensor.gameObject).ToArray();
		}

		IEnumerator StopWithDelay( float delay )
		{
			SetSpeed( ServerState.Speed / 2 );
			yield return WaitFor.Seconds( delay );
			Logger.LogFormat( "{0}: Stopping due to missing thrusters!", Category.Transform, matrixInfo.Matrix.name );
			StopMovement();
		}
	}

	private void RecheckThrusters()
	{
		thrusters = GetComponentsInChildren<ShipThruster>(true).ToList();
	}

	public void RegisterShuttleFuelSystem(ShuttleFuelSystem shuttleFuel)
	{
		this.shuttleFuelSystem = shuttleFuel;
	}

	public void RegisterShuttleGuiScript(GUI_ShuttleControl shuttleGui)
	{
		shuttleControlGUI = shuttleGui;
	}
	public void RegisterCoordReadoutScript(GUI_CoordReadout coordReadout)
	{
		coordReadoutScript = coordReadout;
	}

	public override void FixedUpdateMe()
	{
		if (isServer)
		{
			CheckMovementServer();
		}
		else
		{
			CheckMovementClient();
		}
	//	AnimateMovement();
	}

	///managed by UpdateManager
	public override void LateUpdateMe()
	{
		//finish rotation now that the transform should finally be rotated
		if (!NeedsRotationClient && inProgressRotation != null)
		{
			//client and server logic happens here because server also must wait for the rotation to finish lerping.
			Logger.LogTraceFormat("{0} ending rotation progress to {1}", Category.Matrix, this, inProgressRotation.Value);
			if (isServer)
			{
				MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Server, RotationEvent.End));
			}
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, RotationEvent.End));
			inProgressRotation = null;
			if (pendingInitialRotation && !receivedInitialState)
			{
				receivedInitialState = true;
				pendingInitialRotation = false;
			}
		}

		if (isClient)
		{
			if(coordReadoutScript != null) coordReadoutScript.SetCoords(ServerState.Position);
			if (shuttleControlGUI != null && rcsModeActive != shuttleControlGUI.RcsMode)
			{
				shuttleControlGUI.ClientToggleRcs(rcsModeActive);
			}
		}
	}

	private bool CanMoveTo(Orientation direction)
	{
		Vector3 dir = direction.Vector;

		//		check if next tile is passable
		for (var i = 0; i < SensorPositions.Length; i++)
		{
			var sensor = SensorPositions[i];
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(sensor, matrixInfo, ServerState);

			// Exclude the moving matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos + dir.RoundToInt(), isServer: true,
											collisionType: matrixColliderType, excludeList: excludeList))
			{
				return false;
			}
		}

//		Logger.LogTrace( $"Passing {serverTargetState.Position}->{serverTargetState.Position+dir} ", Category.Matrix );
		return true;
	}

	private bool CanRotateTo(Orientation flyingDirection)
	{
		if (rotationSensorContainerObject == null) { return true; }

		// Feign a rotation using GameObjects for reference
		Transform rotationSensorContainerTransform = rotationSensorContainerObject.transform;
		rotationSensorContainerTransform.rotation = new Quaternion();
		rotationSensorContainerTransform.Rotate(0f, 0f, 90f * ServerState.FlyingDirection.RotationsTo(flyingDirection));

		for (var i = 0; i < RotationSensors.Length; i++)
		{
			var sensor = RotationSensors[i];
			// Need to pass an aggriate local vector in reference to the Matrix GO to get the correct WorldPos
			Vector3 localSensorAggrigateVector = (rotationSensorContainerTransform.localRotation * sensor.transform.localPosition) + rotationSensorContainerTransform.localPosition;
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(localSensorAggrigateVector, matrixInfo, ServerState);

			// Exclude the rotating matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos, isServer: true,
											collisionType: matrixColliderType, includingPlayers: true, excludeList: excludeList))
			{
				return false;
			}
		}

		return true;
	}

	/// Manually set matrix to a specific position.
	[Server]
	public void SetPosition(Vector3 pos, bool notify = true)
	{
		Vector3Int intPos = Vector3Int.RoundToInt(pos);
		serverState.Position = intPos;
		serverTargetState.Position = intPos;
		serverState.Inform = true;
		if (notify)
		{
			NotifyPlayers();
		}
	}

	/// Called when MatrixMoveMessage is received
	public void UpdateClientState(MatrixState newState)
	{
		var oldState = clientState;

		clientState = newState;
		Logger.LogTraceFormat("{0} setting client / client target state from message {1}", Category.Matrix, this, newState);


		if (!Equals(oldState.FacingDirection, newState.FacingDirection))
		{
			if (!receivedInitialState && !pendingInitialRotation)
			{
				pendingInitialRotation = true;
			}
			inProgressRotation = oldState.FacingDirection.OffsetTo(newState.FacingDirection);
			Logger.LogTraceFormat("{0} starting rotation progress to {1}", Category.Matrix, this, newState.FacingDirection);
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, RotationEvent.Start));
		}

		if (!oldState.IsMoving && newState.IsMoving)
		{
			MatrixMoveEvents.OnStartMovementClient.Invoke();
		}

		if (oldState.IsMoving && !newState.IsMoving)
		{
			MatrixMoveEvents.OnStopMovementClient.Invoke();
		}

		if ((int) oldState.Speed != (int) newState.Speed)
		{
			MatrixMoveEvents.OnSpeedChange.Invoke(oldState.Speed, newState.Speed);
		}

		if (!receivedInitialState && !pendingInitialRotation)
		{
			receivedInitialState = true;
		}
	}

	/// Schedule notification for the next ServerPositionsMatch
	/// And check if it's able to send right now
	[Server]
	private void RequestNotify()
	{
		serverTargetState.Inform = true;
		TryNotifyPlayers();
	}

	///	Inform players when on integer position
	[Server]
	private void TryNotifyPlayers()
	{
		if (ServerPositionsMatch)
		{
//				When serverState reaches its planned destination,
//				embrace all other updates like changed speed and rotation
			serverState = serverTargetState;
			Logger.LogTraceFormat("{0} setting server state from target state {1}", Category.Matrix, this, serverState);
			NotifyPlayers();
		}
	}

	///  Currently sending to everybody, but should be sent to nearby players only
	[Server]
	private void NotifyPlayers()
	{
		//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
		if (!IsMovingServer || serverState.Inform)
		{
			serverState.RotationTime = rotTime;

			//fixme: this whole class behaves like ass!
			if ( serverState.RotationTime != serverTargetState.RotationTime )
			{ //Doesn't guarantee that matrix will stop
				MatrixMoveMessage.SendToAll(gameObject, serverState);
			} else
			{ //Ends up in instant rotations
				MatrixMoveMessage.SendToAll(gameObject, serverTargetState);
			}
			//Clear inform flags
			serverTargetState.Inform = false;
			serverState.Inform = false;
		}
	}

	///     Sync with new player joining
	/// <param name="playerGameObject">player to send to</param>
	/// <param name="rotateImmediate">(for init) rotation should be applied immediately if true</param>
	[Server]
	public void UpdateNewPlayer(NetworkConnection playerConn, bool rotateImmediate = false)
	{
		serverState.RotationTime = rotateImmediate ? 0 : rotTime;
		MatrixMoveMessage.Send(playerConn, gameObject, serverState);
	}

	///Only change orientation if rotation is finished
	[Server]
	public void TryRotate(bool clockwise)
	{
		if (!IsRotatingServer)
		{
			Steer(clockwise);
		}
	}

	/// <summary>
	/// Steer 90 degrees in a direction and change flying direction to match
	/// </summary>
	/// <param name="clockwise"></param>
	[Server]
	public void Steer(bool clockwise)
	{
		SteerTo(serverTargetState.FacingDirection.Rotate(clockwise ? 1 : -1));
	}

	/// <summary>
	/// Change facing and flying direction to match specified direction if possible.
	/// If blocked, returns false.
	/// </summary>
	/// <param name="desiredOrientation"></param>
	[Server]
	public bool SteerTo(Orientation desiredOrientation)
	{
		if (CanRotateTo(desiredOrientation))
		{
			serverTargetState.FacingDirection = desiredOrientation;
			serverTargetState.FlyingDirection = desiredOrientation;
			Logger.LogTraceFormat("{0} server target facing / flying {1}", Category.Matrix, this, desiredOrientation);

			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, serverState.FacingDirection.OffsetTo(desiredOrientation), NetworkSide.Server, RotationEvent.Start));

			RequestNotify();
			return true;
		}
		return false;
	}


	/// Changes flying direction without rotating the shuttle, for use in reversing in EscapeShuttle
	[Server]
	public void ChangeFlyingDirection(Orientation newFlyingDirection)
	{
		serverTargetState.FlyingDirection = newFlyingDirection;
		Logger.LogTraceFormat("{0} server target flying {1}", Category.Matrix, this, newFlyingDirection);
	}

	/// Changes facing direction without changing flying direction, for use in reversing in EscapeShuttle
	[Server]
	public bool ChangeFacingDirection(Orientation newFacingDirection)
	{
		if (CanRotateTo(newFacingDirection))
		{
			serverTargetState.FacingDirection = newFacingDirection;
			Logger.LogTraceFormat("{0} server target facing  {1}", Category.Matrix, this, newFacingDirection);

			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, serverState.FacingDirection.OffsetTo(newFacingDirection), NetworkSide.Server, RotationEvent.Start));

			RequestNotify();
			return true;
		}
		return false;
	}

	private Vector3 Target = TransformState.HiddenPos;

	/// Makes matrix start moving towards given world pos
	[Server]
	public void AutopilotTo(Vector2 position)
	{
		Target = position;
		StartCoroutine(TravelToTarget());
	}

	///Zero means 100% accurate, but will lead to peculiar behaviour (autopilot not reacting fast enough on high speed -> going back/in circles etc)
	private int AccuracyThreshold = 1;

	public void SetAccuracy(int newAccuracy)
	{
		AccuracyThreshold = newAccuracy;
	}

	private IEnumerator TravelToTarget()
	{
		if (IsAutopilotEngaged)
		{
			var pos = serverState.Position;
			if (Vector3.Distance(pos, Target) <= AccuracyThreshold)
			{
				StopMovement();
				yield break;
			}

			Orientation currentDir = serverState.FlyingDirection;

			Vector3 xProjection = Vector3.Project(pos, Vector3.right);
			int xProjectionX = (int) xProjection.x;
			int targetX = (int) Target.x;

			Vector3 yProjection = Vector3.Project(pos, Vector3.up);
			int yProjectionY = (int) yProjection.y;
			int targetY = (int) Target.y;

			bool xNeedsChange = Mathf.Abs(xProjectionX - targetX) > AccuracyThreshold;
			bool yNeedsChange = Mathf.Abs(yProjectionY - targetY) > AccuracyThreshold;

			Orientation xDesiredDir = targetX - xProjectionX > 0 ? Orientation.Right : Orientation.Left;
			Orientation yDesiredDir = targetY - yProjectionY > 0 ? Orientation.Up : Orientation.Down;

			if (xNeedsChange || yNeedsChange)
			{
				int xRotationsTo = xNeedsChange ? currentDir.RotationsTo(xDesiredDir) : int.MaxValue;
				int yRotationsTo = yNeedsChange ? currentDir.RotationsTo(yDesiredDir) : int.MaxValue;

				//don't rotate if it's not needed
				if (xRotationsTo != 0 && yRotationsTo != 0)
				{
					//if both need change determine faster rotation first
					SteerTo(xRotationsTo < yRotationsTo ? xDesiredDir : yDesiredDir);
					//wait till it rotates
					yield return WaitFor.Seconds(1);
				}
			}

			if (!serverState.IsMoving)
			{
				StartMovement();
			}

			//Relaunching self once in a while as CheckMovementServer check can fail in rare occasions
			yield return WaitFor.Seconds(1);
			StartCoroutine(TravelToTarget());
		}

		yield return null;
	}

	//Searches the matrix for RcsThrusters
	public void CacheRcs()
	{
		ClearRcsCache();
		foreach(Transform t in matrixInfo.Objects)
		{
			if (t.tag.Equals("Rcs"))
			{
				CacheRcs(t.GetComponent<DirectionalRotatesParent>().MappedOrientation,
					t.GetComponent<RcsThruster>());
			}
		}
	}

	void CacheRcs(OrientationEnum mappedOrientation, RcsThruster thruster)
	{
		if (InitialFacing == Orientation.Up)
		{
			if(mappedOrientation == OrientationEnum.Up) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) starBoardRcsThrusters.Add(thruster);
		}

		if (InitialFacing == Orientation.Right)
		{
			if(mappedOrientation == OrientationEnum.Up) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) bowRcsThrusters.Add(thruster);
		}

		if (InitialFacing == Orientation.Down)
		{
			if(mappedOrientation == OrientationEnum.Up) sternRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) portRcsThrusters.Add(thruster);
		}

		if (InitialFacing == Orientation.Left)
		{
			if(mappedOrientation == OrientationEnum.Up) starBoardRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Down) portRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Right) bowRcsThrusters.Add(thruster);
			if(mappedOrientation == OrientationEnum.Left) sternRcsThrusters.Add(thruster);
		}
	}

	void ClearRcsCache()
	{
		bowRcsThrusters.Clear();
		sternRcsThrusters.Clear();
		portRcsThrusters.Clear();
		starBoardRcsThrusters.Clear();
	}

#if UNITY_EDITOR
	//Visual debug
	private Vector3 size1 = Vector3.one;
	private Vector3 size2 = new Vector3(0.9f, 0.9f, 0.9f);
	private Vector3 size3 = new Vector3(0.8f, 0.8f, 0.8f);
	private Color color1 = Color.red;
	private Color color2 = DebugTools.HexToColor("81a2c7");
	private Color color3 = Color.white;

	private void OnDrawGizmos()
	{
		if ( !Application.isPlaying )
		{ //Showing matrix pivot if game is stopped
			Gizmos.color = color1.WithAlpha( 0.6f );
			Gizmos.DrawCube(transform.position, Vector3.one );
			Gizmos.color = color1;
			Gizmos.DrawWireCube(transform.position, Vector3.one );

			DebugGizmoUtils.DrawArrow(transform.position, ServerState.FlyingDirection.Vector*2);
			return;
		}

		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = ServerState.Position;
		Gizmos.DrawWireCube(serverPos, size1);
		if (ServerState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(serverPos + Vector3.right / 3, ServerState.FlyingDirection.Vector * ServerState.Speed);
			DebugGizmoUtils.DrawText(ServerState.Speed.ToString(), serverPos + Vector3.right, 15);
		}
	}
#endif
}

public enum UIType
{
	Default = 0,
	Nanotrasen = 1,
	Syndicate = 2
};
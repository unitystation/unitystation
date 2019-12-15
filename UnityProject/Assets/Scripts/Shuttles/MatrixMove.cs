using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using UnityEngine.Serialization;

/// <summary>
/// Behavior which allows an entire matrix to move and rotate (and be synced over the network).
/// This behavior must go on a gameobject that is the parent of the gameobject that has the actual Matrix component.
/// </summary>
public class MatrixMove : ManagedNetworkBehaviour
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
	/// <summary>
	/// Initial facing of the ship as mapped in the editor.
	/// </summary>
	public Orientation InitialFacing => Orientation.FromEnum(initialFacing);

	[Tooltip("Max flying speed of this matrix.")]
	[FormerlySerializedAs("maxSpeed")]
	public float MaxSpeed = 20f;

	[Tooltip("Whether safety is currently on, preventing collisions when sensors detect them.")]
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

	//server-only values
	public MatrixState State => serverState;
	public bool IsMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	//client-only values
	public MatrixState ClientState => clientState;
	private MatrixInfo matrixInfo;
	public MatrixInfo MatrixInfo => matrixInfo;
	private ShuttleFuelSystem shuttleFuelSystem;
	public ShuttleFuelSystem ShuttleFuelSystem => shuttleFuelSystem;

	/// <summary>
	/// If it is currently fuelled
	/// </summary>
	[NonSerialized]
	public bool IsFueled;

	/// <summary>
	/// Requires Fuel
	/// </summary>
	[NonSerialized]
	public bool RequiresFuel;


	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool IsRotatingServer => NeedsRotation; //todo: calculate rotation time on server instead
	private bool IsAutopilotEngaged => Target != TransformState.HiddenPos;
	private bool IsMovingClient => clientState.IsMoving && clientState.Speed > 0f;
	/// <summary>
	/// Does current transform rotation not yet match the target rotation, and thus this matrix's transform needs to
	/// be rotated to match the target?
	/// </summary>
	private bool NeedsRotation =>
		Quaternion.Angle(transform.rotation, InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion) != 0;
	private bool ClientPositionsMatch => clientTargetState.Position == clientState.Position;


	private MatrixPositionFilter matrixPositionFilter = new MatrixPositionFilter();

	///used for syncing with players, matters only for server
	private MatrixState serverState = MatrixState.Invalid;
	/// future state that collects all changes
	private MatrixState serverTargetState = MatrixState.Invalid;
	private Coroutine floatingSyncHandle;
	/// <summary>
	/// Tracks whether we've recieved our initial settings from the server.
	/// </summary>
	private bool clientInitialized;
	/// Is only present to match server's flight routines
	private MatrixState clientTargetState = MatrixState.Invalid;
	private List<ShipThruster> thrusters = new List<ShipThruster>();
	///client's transform, can get dirty/predictive
	private MatrixState clientState = MatrixState.Invalid;
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
	private int moveCur = -1;
	private int moveLimit = -1;

	public override void OnStartClient()
	{
		SyncPivot(pivot);
		SyncInitialPosition(initialPosition);
	}

	public override void OnStartServer()
	{
		InitServerState();

		MatrixMoveEvents.OnStartMovementServer.AddListener( () =>
		{
			if ( floatingSyncHandle == null )
			{
				this.StartCoroutine( FloatingAwarenessSync(), ref floatingSyncHandle );
			}
		} );
		MatrixMoveEvents.OnStopMovementServer.AddListener( () => this.TryStopCoroutine( ref floatingSyncHandle ) );

		base.OnStartServer();
		NotifyPlayers();
	}

	[Server]
	private void InitServerState()
	{
		serverState.FlyingDirection = InitialFacing;
		serverState.FacingDirection = InitialFacing;

		Vector3Int initialPositionInt =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		SyncInitialPosition(initialPositionInt);

		var child = transform.GetChild( 0 );
		matrixInfo = MatrixManager.Get( child.gameObject );
		var childPosition = Vector3Int.CeilToInt(new Vector3(child.transform.position.x, child.transform.position.y, 0));
		SyncPivot(initialPosition - childPosition);

		Logger.LogTraceFormat("{0}: pivot={1} initialPos={2}", Category.Matrix, gameObject.name,
			pivot, initialPositionInt);
		serverState.Speed = 1f;
		serverState.Position = initialPosition;
		serverTargetState = serverState;
		clientState = serverState;
		clientTargetState = serverState;

		thrusters = GetComponentsInChildren<ShipThruster>(true).ToList();
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
			           $" direction is {State.FlyingDirection}", Category.Matrix);
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
			SetSpeed( State.Speed / 2 );
			yield return WaitFor.Seconds( delay );
			Logger.LogFormat( "{0}: Stopping due to missing thrusters!", Category.Transform, matrixInfo.Matrix.name );
			StopMovement();
		}
	}

	public void RegisterShuttleFuelSystem(ShuttleFuelSystem shuttleFuel)
	{
		this.shuttleFuelSystem = shuttleFuel;
	}

	public void RegisterCoordReadoutScript(GUI_CoordReadout coordReadout)
	{
		this.coordReadoutScript = coordReadout;
	}

	private void SyncInitialPosition(Vector3 initialPos)
	{
		this.initialPosition = initialPos.RoundToInt();
	}

	private void SyncPivot(Vector3 pivot)
	{
		this.pivot = pivot.RoundToInt();
	}

	/// <summary>
	/// Send current position of space floating player to clients every second in case their reproduction is wrong
	/// </summary>
	private IEnumerator FloatingAwarenessSync()
	{
		yield return WaitFor.Seconds(1);
		serverState.Inform = true;
		NotifyPlayers();
		this.RestartCoroutine( FloatingAwarenessSync(), ref floatingSyncHandle );
	}

	///managed by UpdateManager
	public override void UpdateMe()
	{
		if (isServer)
		{
			CheckMovementServer();
		}

		AnimateMovement();
	}

	///managed by UpdateManager
	public override void LateUpdateMe()
	{
		//finish rotation now that the transform should finally be rotated
		if (!NeedsRotation && inProgressRotation != null)
		{
			//client and server logic happens here because server also must wait for the rotation to finish lerping.
			if (isServer)
			{
				MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Server, false));
			}
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, false));
			inProgressRotation = null;
		}

		if (isClient && coordReadoutScript != null)
		{
			coordReadoutScript.SetCoords(clientState.Position);
		}
	}

	[Server]
	public void ToggleMovement()
	{
		if (IsMovingServer)
		{
			StopMovement();
		}
		else
		{
			StartMovement();
		}
	}

	/// Start moving. If speed was zero, it'll be set to 1
	[Server]
	public void StartMovement()
	{
		if (IsFueled || !RequiresFuel)
		{
			//Setting speed if there is none
			if (serverTargetState.Speed <= 0)
			{
				SetSpeed(1);
			}

			Logger.LogTrace(gameObject.name + " started moving with speed " + serverTargetState.Speed, Category.Matrix);
			serverTargetState.IsMoving = true;
			MatrixMoveEvents.OnStartMovementServer.Invoke();

			RequestNotify();
		}
	}

	/// Stop movement
	[Server]
	public void StopMovement()
	{
		Logger.LogTrace(gameObject.name+ " stopped movement", Category.Matrix);
		serverTargetState.IsMoving = false;
		MatrixMoveEvents.OnStopMovementServer.Invoke();

		//To stop autopilot
		DisableAutopilotTarget();
		TryNotifyPlayers();
	}

	/// Move for n tiles, regardless of direction, and stop
	[Server]
	public void MoveFor(int tiles)
	{
		if (tiles < 1)
		{
			tiles = 1;
		}

		if (!IsMovingServer)
		{
			StartMovement();
		}

		moveCur = 0;
		moveLimit = tiles;
	}

	/// Checks if it still can move according to MoveFor limits.
	/// If true, increment move count
	[Server]
	private bool CanMoveFor()
	{
		if (moveCur == moveLimit && moveCur != -1)
		{
			moveCur = -1;
			moveLimit = -1;
			return false;
		}

		moveCur++;
		return true;
	}

	/// Changes flying direction without rotating the shuttle, for use in reversing in EscapeShuttle.cs
	[Server]
	public void ChangeFlyingDirection(Orientation newFlyingDirection)
	{
		serverTargetState.FlyingDirection = newFlyingDirection;
	}

	/// Call to stop chasing target
	[Server]
	public void DisableAutopilotTarget()
	{
		Target = TransformState.HiddenPos;
	}

	/// Adjust current ship's speed with a relative value
	[Server]
	public void AdjustSpeed(float relativeValue)
	{
		float absSpeed = serverTargetState.Speed + relativeValue;
		SetSpeed(absSpeed);
	}

	/// Set ship's speed using absolute value. it will be truncated if it's out of bounds
	[Server]
	public void SetSpeed(float absoluteValue)
	{
		if (absoluteValue <= 0)
		{
			//Stop movement if speed is zero or below
			serverTargetState.Speed = 0;
			if (serverTargetState.IsMoving)
			{
				StopMovement();
			}

			return;
		}

		if (absoluteValue > MaxSpeed)
		{
			Logger.LogWarning($"MaxSpeed {MaxSpeed} reached, not going further", Category.Matrix);
			if (serverTargetState.Speed >= MaxSpeed)
			{
				//Not notifying people if some dick is spamming "increase speed" button at max speed
				return;
			}

			serverTargetState.Speed = MaxSpeed;
		}
		else
		{
			serverTargetState.Speed = absoluteValue;
		}

		//do not send speed updates when not moving
		if (serverTargetState.IsMoving)
		{
			RequestNotify();
		}
	}

	/// <summary>
	/// Performs the rotation / movement animation on all clients and server. Called every UpdateMe()
	/// </summary>
	private void AnimateMovement()
	{
		if (Equals(clientState, MatrixState.Invalid))
		{
			return;
		}

		if (NeedsRotation)
		{
			//rotate our transform to our new facing direction
			if (clientState.RotationTime != 0)
			{
				//animate rotation
				transform.rotation =
					Quaternion.RotateTowards(transform.rotation,
						inProgressRotation.Value.Quaternion,
						Time.deltaTime * clientState.RotationTime);
			}
			else
			{
				//rotate instantly
				transform.rotation = inProgressRotation.Value.Quaternion;
			}
		}
		else if (IsMovingClient)
		{
			//Only move target if rotation is finished
			SimulateStateMovement();
		}

		//finish rotation (rotation event will be fired in lateupdate
		if (!NeedsRotation && inProgressRotation != null)
		{
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = inProgressRotation.Value.Quaternion;
		}

		//Lerp
		if (clientState.Position != transform.position)
		{
			float distance = Vector3.Distance(clientState.Position, transform.position);

			//Teleport (Greater then 30 unity meters away from server target):
			if (distance > 30f)
			{
				matrixPositionFilter.FilterPosition(transform, clientState.Position, clientState.FlyingDirection);
				return;
			}

			transform.position = Vector3.MoveTowards( transform.position, clientState.Position,
				clientState.Speed * Time.deltaTime * Mathf.Clamp( distance, 1, distance ));

			//If stopped then lerp to target (snap to grid)
			if (!clientState.IsMoving )
			{
				if ( clientState.Position == transform.position )
				{
					MatrixMoveEvents.OnFullStopClient.Invoke();
				}
				if ( distance > 0f )
				{
					//TODO: Why is this needed? Seems weird.
					matrixPositionFilter.SetPosition(transform.position);
					return;
				}
			}

			matrixPositionFilter.FilterPosition(transform, transform.position, clientState.FlyingDirection);
		}
	}

	/// Serverside movement routine
	[Server]
	private void CheckMovementServer()
	{
		//Not doing any serverside movement while rotating
		if (IsRotatingServer)
		{
			return;
		}

		//ServerState lerping to its target tile
		if (!ServerPositionsMatch)
		{
			serverState.Position =
				Vector3.MoveTowards(serverState.Position,
					serverTargetState.Position,
					serverState.Speed * Time.deltaTime);
			TryNotifyPlayers();
		}

		bool isGonnaStop = !serverTargetState.IsMoving;
		if (!IsMovingServer || isGonnaStop || !ServerPositionsMatch)
		{
			return;
		}

		if (CanMoveFor() && (!SafetyProtocolsOn || CanMoveTo(serverTargetState.FlyingDirection)))
		{
			var goal = Vector3Int.RoundToInt(serverState.Position + serverTargetState.FlyingDirection.Vector);
			//keep moving
			serverTargetState.Position = goal;
			if (IsAutopilotEngaged && ((int) serverState.Position.x == (int) Target.x
			                           || (int) serverState.Position.y == (int) Target.y))
			{
				StartCoroutine(TravelToTarget());
			}
		}
		else
		{
//			Logger.LogTrace( "Stopping due to safety protocols!",Category.Matrix );
			StopMovement();
			TryNotifyPlayers();
		}
	}

	private bool CanMoveTo(Orientation direction)
	{
		Vector3 dir = direction.Vector;

		//		check if next tile is passable
		for (var i = 0; i < SensorPositions.Length; i++)
		{
			var sensor = SensorPositions[i];
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(sensor, matrixInfo, serverTargetState);

			// Exclude the moving matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos + dir.RoundToInt(), isServer: true,
											collisionType: matrixColliderType, excludeList: excludeList))
			{
				Logger.LogTrace(
					$"Can't pass {serverTargetState.Position}->{serverTargetState.Position + dir} (because {sensorPos}->{sensorPos + dir})!",
					Category.Matrix);
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
		rotationSensorContainerTransform.Rotate(0f, 0f, 90f * State.FlyingDirection.RotationsTo(flyingDirection));

		for (var i = 0; i < RotationSensors.Length; i++)
		{
			var sensor = RotationSensors[i];
			// Need to pass an aggriate local vector in reference to the Matrix GO to get the correct WorldPos
			Vector3 localSensorAggrigateVector = (rotationSensorContainerTransform.localRotation * sensor.transform.localPosition) + rotationSensorContainerTransform.localPosition;
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(localSensorAggrigateVector, matrixInfo, serverTargetState);

			// Exclude the rotating matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos, isServer: true,
											collisionType: matrixColliderType, includingPlayers: true, excludeList: excludeList))
			{
				Logger.LogTrace(
					$"Can't rotate at {serverTargetState.Position}->{serverTargetState.Position } (because {sensorPos} is occupied)!",
					Category.Matrix);
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
		clientTargetState = newState;

		if (!Equals(oldState.FacingDirection, newState.FacingDirection))
		{
			inProgressRotation = oldState.FacingDirection.OffsetTo(newState.FacingDirection);
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, true));
		}

		if (!clientInitialized)
		{
			clientInitialized = true;
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
	}

	///predictive perpetual flying
	private void SimulateStateMovement()
	{
		//ClientState lerping to its target tile
		if (!ClientPositionsMatch)
		{
			clientState.Position =
				Vector3.MoveTowards(clientState.Position,
					clientTargetState.Position,
					clientState.Speed * Time.deltaTime);
		}

		if (IsMovingClient && !NeedsRotation)
		{
			Vector3Int goal = Vector3Int.RoundToInt(clientState.Position + clientTargetState.FlyingDirection.Vector);
			//keep moving
			if (ClientPositionsMatch)
			{
				clientTargetState.Position = goal;
			}
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
	public void NotifyPlayer(GameObject playerGameObject, bool rotateImmediate = false)
	{
		serverState.RotationTime = rotateImmediate ? 0 : rotTime;
		MatrixMoveMessage.Send(playerGameObject, gameObject, serverState);
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
			inProgressRotation = serverState.FacingDirection.OffsetTo(desiredOrientation);
			serverTargetState.FacingDirection = desiredOrientation;
			serverTargetState.FlyingDirection = desiredOrientation;

			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Server, true));

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

			DebugGizmoUtils.DrawArrow(transform.position, clientState.FlyingDirection.Vector*2);
			return;
		}

		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = serverState.Position;
		Gizmos.DrawWireCube(serverPos, size1);
		if (serverState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(serverPos + Vector3.right / 3, serverState.FlyingDirection.Vector * serverState.Speed);
			DebugGizmoUtils.DrawText(serverState.Speed.ToString(), serverPos + Vector3.right, 15);
		}

		//serverTargetState
		Gizmos.color = color2;
		Vector3 serverTargetPos = serverTargetState.Position;
		Gizmos.DrawWireCube(serverTargetPos, size2);
		if (serverTargetState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(serverTargetPos, serverTargetState.FlyingDirection.Vector * serverTargetState.Speed);
			DebugGizmoUtils.DrawText(serverTargetState.Speed.ToString(), serverTargetPos + Vector3.down, 15);
		}

		//clientState
		Gizmos.color = color3;
		Vector3 pos = clientState.Position;
		Gizmos.DrawWireCube(pos, size3);
		if (clientState.IsMoving)
		{
			DebugGizmoUtils.DrawArrow(pos + Vector3.left / 3, clientState.FlyingDirection.Vector * clientState.Speed);
			DebugGizmoUtils.DrawText(clientState.Speed.ToString(), pos + Vector3.left, 15);
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
﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Light2D;
using UnityEngine;
using Mirror;
using UnityEngine.Serialization;
using UI.Objects.Shuttles;
using Systems.Shuttles;
using Messages.Client.NewPlayer;
using Messages.Server;

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
	private OrientationEnum initialFacing = OrientationEnum.Down;
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
	public MatrixState ServerState => serverState;
	public bool IsMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	//client-only values
	public MatrixState ClientState => clientState;
	private MatrixInfo matrixInfo;
	public MatrixInfo MatrixInfo => matrixInfo;
	private ShuttleFuelSystem shuttleFuelSystem;
	public ShuttleFuelSystem ShuttleFuelSystem => shuttleFuelSystem;
	/// <summary>
	/// Gets the rotation offset this matrix has from its initial mapped
	/// facing.
	/// </summary>
	public RotationOffset FacingOffsetFromInitial => ClientState.FacingOffsetFromInitial(this);

	/// <summary>
	/// If it is currently fuelled
	/// </summary>
	[NonSerialized]
	public bool IsFueled;

	public bool IsForceStopped;

	[Tooltip("Does it require fuel in order to fly?")]
	public bool RequiresFuel;

	[SyncVar(hook = nameof(OnRcsActivated))]
	[HideInInspector]
	public bool rcsModeActive;

	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool IsRotatingServer => NeedsRotationClient; //todo: calculate rotation time on server instead
	private bool IsAutopilotEngaged => Target != TransformState.HiddenPos;
	private bool IsMovingClient => clientState.IsMoving && clientState.Speed > 0f;

	/// <summary>
	/// Dictionary containing lists of RCS thrusters.
	/// [If you want to remove all thrusters clear the lists not dictionary]
	/// </summary>
	private Dictionary<OrientationEnum, List<RcsThruster>> rcsThrusters = new Dictionary<OrientationEnum, List<RcsThruster>>
	{
		{OrientationEnum.Up,    new List<RcsThruster>()},
		{OrientationEnum.Down,  new List<RcsThruster>()},
		{OrientationEnum.Left,  new List<RcsThruster>()},
		{OrientationEnum.Right, new List<RcsThruster>()}
	};

	/// <summary>
	/// shuttle position when starting RCS movement
	/// </summary>
	private Vector2Int rcsMovementStartPosition;

	/// <summary>
	/// position on which player should be after the start of RCS
	/// position on which shuttle should be located at the end of RCS movement
	/// </summary>
	private Vector2Int rcsMovementTargetPosition;

	/// <summary>
	/// TODO
	/// </summary>
	private bool canClientUseRcs = true;

	private bool canServerUseRcs = true;

	/// <summary>
	/// Does current transform rotation not yet match the client matrix state rotation, and thus this matrix's transform needs to
	/// be rotated to match the target?
	/// </summary>
	private bool NeedsRotationClient =>
		Quaternion.Angle(transform.rotation, InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion) != 0;


	private MatrixPositionFilter matrixPositionFilter = new MatrixPositionFilter();

	///used for syncing with players, matters only for server
	private MatrixState serverState = MatrixState.Invalid;
	/// future state that collects all changes
	private MatrixState serverTargetState = MatrixState.Invalid;
	private Coroutine floatingSyncHandle;
	///client's transform, can get dirty/predictive
	private MatrixState clientState = MatrixState.Invalid;

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
	public RotationOffset? inProgressRotation;

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

	[FormerlySerializedAs("NoConsole"),Tooltip("Disable the ability for players to use a shuttleconsole to control this matrix")]
	public bool IsNotPilotable = false;

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

			var child = transform.GetChild(0);
			matrixInfo = MatrixManager.Get(child.gameObject);
		}
	}

	[Server]
	private void InitServerState()
	{
		serverState.FlyingDirection = InitialFacing;
		serverState.FacingDirection = InitialFacing;
		Logger.LogTraceFormat("{0} server initial facing / flying {1}", Category.Shuttles, this, InitialFacing);

		Vector3Int initialPositionInt =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		SyncInitialPosition(initialPosition, initialPositionInt);

		var child = transform.GetChild(0);
		matrixInfo = MatrixManager.Get(child.gameObject);
		var childPosition = Vector3Int.CeilToInt(new Vector3(child.transform.position.x, child.transform.position.y, 0));
		SyncPivot(pivot, initialPosition - childPosition);

		Logger.LogTraceFormat("{0}: pivot={1} initialPos={2}", Category.Shuttles, gameObject.name,
			pivot, initialPositionInt);
		serverState.Speed = 1f;
		serverState.Position = initialPosition;
		serverTargetState = serverState;
		clientState = serverState;

		RecheckThrusters();
		if (thrusters.Count > 0)
		{
			Logger.LogFormat("{0}: Initializing {1} thrusters!", Category.Shuttles, matrixInfo.Matrix.name, thrusters.Count);
			foreach (var thruster in thrusters)
			{
				var integrity = thruster.GetComponent<Integrity>();
				if (integrity)
				{
					integrity.OnWillDestroyServer.AddListener(destructionInfo =>
				   {
					   if (thrusters.Contains(thruster))
					   {
						   thrusters.Remove(thruster);
					   }

					   if (thrusters.Count == 0 && IsMovingServer)
					   {
						   Logger.LogFormat("All thrusters were destroyed! Stopping {0} soon!", Category.Shuttles, matrixInfo.Matrix.name);
						   StartCoroutine(StopWithDelay(1f));
					   }
				   });
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
					   $" direction is {ServerState.FlyingDirection}", Category.Shuttles);
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

		IEnumerator StopWithDelay(float delay)
		{
			SetSpeed(ServerState.Speed / 2);
			yield return WaitFor.Seconds(delay);
			Logger.LogFormat("{0}: Stopping due to missing thrusters!", Category.Shuttles, matrixInfo.Matrix.name);
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
		this.coordReadoutScript = coordReadout;
	}

	private void SyncInitialPosition(Vector3 oldPos, Vector3 initialPos)
	{
		this.initialPosition = initialPos.RoundToInt();
	}

	private void SyncPivot(Vector3 oldPivot, Vector3 pivot)
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
		this.RestartCoroutine(FloatingAwarenessSync(), ref floatingSyncHandle);
	}

	///managed by UpdateManager
	public override void FixedUpdateMe()
	{
		if (isServer)
		{
			CheckMovementServer();
		}
	}

	public override void UpdateMe()
	{
		AnimateMovement();
	}

	///managed by UpdateManager
	public override void LateUpdateMe()
	{
		//finish rotation now that the transform should finally be rotated
		if (!NeedsRotationClient && inProgressRotation != null)
		{
			//client and server logic happens here because server also must wait for the rotation to finish lerping.
			Logger.LogTraceFormat("{0} ending rotation progress to {1}", Category.Shuttles, this, inProgressRotation.Value);
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
			if (coordReadoutScript != null) coordReadoutScript.SetCoords(clientState.Position);
			if (shuttleControlGUI != null && rcsModeActive != shuttleControlGUI.RcsMode)
			{
				shuttleControlGUI.ClientToggleRcs(rcsModeActive);

				// int.MaxValue instead of zero to avoid bugs when shuttle is on position (0, 0)
				rcsMovementStartPosition = new Vector2Int(int.MaxValue, int.MaxValue);
				rcsMovementTargetPosition = new Vector2Int(int.MaxValue, int.MaxValue);
			}
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

	[Server]
	public void ToggleRcs(bool on)
	{
		rcsModeActive = on;
		if (on)
		{
			//Refresh Rcs
			CacheRcs();
		}
	}

	/// Start moving. If speed was zero, it'll be set to 1
	[Server]
	public void StartMovement()
	{
		if (!HasWorkingThrusters)
		{
			RecheckThrusters();
		}

		if (IsForceStopped || rcsModeActive) return;

		//Not allowing movement without any thrusters:
		if (HasWorkingThrusters && (IsFueled || !RequiresFuel))
		{
			//Setting speed if there is none
			if (serverTargetState.Speed <= 0)
			{
				SetSpeed(1);
			}

			Logger.LogTrace(gameObject.name + " started moving with speed " + serverTargetState.Speed, Category.Shuttles);
			serverTargetState.IsMoving = true;
			MatrixMoveEvents.OnStartMovementServer.Invoke();

			RequestNotify();
		}
	}

	[Server]
	public void RcsStartMovementServer(Orientation flyingDirection)
	{
		moveLimit = moveCur + 1;

		serverTargetState.IsMoving = true;
		serverTargetState.Speed = 1;
		ChangeFlyingDirection(flyingDirection);

		RequestNotify();

		MatrixMoveEvents.OnStartMovementServer.Invoke();
	}

	[Client]
	public void RcsStartMovementClient(Orientation flyingDirection)
	{
		rcsMovementStartPosition = Vector2Int.RoundToInt(transform.position);
		rcsMovementTargetPosition = rcsMovementStartPosition + flyingDirection.VectorInt;

		clientState.Speed = 1;
		clientState.FlyingDirection = flyingDirection;
		clientState.IsMoving = true;

		MatrixMoveEvents.OnStartMovementClient.Invoke();
	}

	[Client]
	public void OnRcsActivated(bool oldValue, bool newValue)
	{
		if (newValue)
		{
			CacheRcs();
		}
	}

	/// Stop movement
	[Server]
	public void StopMovement()
	{
		Logger.LogTrace(gameObject.name + " stopped movement", Category.Shuttles);
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
			Logger.LogWarning($"MaxSpeed {MaxSpeed} reached, not going further", Category.Shuttles);
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

		if (NeedsRotationClient)
		{
			//rotate our transform to our new facing direction
			if (clientState.RotationTime != 0)
			{
				//animate rotation
				transform.rotation =
					Quaternion.RotateTowards(transform.rotation,
						 InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion,
						Time.deltaTime * clientState.RotationTime);
			}
			else
			{
				//rotate instantly
				transform.rotation = InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion;
			}
		}
		else if (IsMovingClient)
		{
			//Only move target if rotation is finished
			//predict client state because we don't get constant updates when flying in one direction.
			clientState.Position += (clientState.Speed * Time.deltaTime) * clientState.FlyingDirection.Vector;
			if (rcsModeActive && clientState.IsMoving)
			{
				// if shuttle is close to reach target position,
				if (Vector2.Distance(clientState.Position, rcsMovementTargetPosition) <= 0.02f)
				{
					clientState.Position = new Vector3(rcsMovementTargetPosition.x, rcsMovementTargetPosition.y, clientState.Position.z);
					clientState.Speed = 0;
					clientState.IsMoving = false;

					MatrixMoveEvents.OnStopMovementClient.Invoke();
				}
			}
		}

		//finish rotation (rotation event will be fired in lateupdate
		if (!NeedsRotationClient && inProgressRotation != null)
		{
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = InitialFacing.OffsetTo(clientState.FacingDirection).Quaternion;
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

			transform.position = clientState.Position;

			//If stopped then lerp to target (snap to grid)
			if (!clientState.IsMoving)
			{
				if (clientState.Position == transform.position)
				{
					MatrixMoveEvents.OnFullStopClient.Invoke();
				}
				if (distance > 0f)
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

		Vector3? actualNewPosition = null;
		if (!ServerPositionsMatch)
		{
			//some special logic needs to fire when we exactly reach our target tile,
			//but we want movement to continue as far as it should based on deltaTime
			//despite reaching / exceeding the target tile. So we save the actual new position
			//here and only update serverState.Position after that special logic has run.
			//Otherwise, movement speed will fluctuate slightly due to discarding excess movement that happens
			//when reaching an exact tile position and result in server movement jerkiness and inconsistent client predicted movement.

			//actual position we should reach this update, regardless of if we passed through the target position
			actualNewPosition = serverState.Position +
								serverState.FlyingDirection.Vector * (serverState.Speed * Time.deltaTime);
			//update position without passing the target position
			serverState.Position =
				Vector3.MoveTowards(serverState.Position,
					serverTargetState.Position,
					serverState.Speed * Time.deltaTime);

			//At this point, if serverState.Position reached an exact tile position,
			//you can see that actualNewPosition != serverState.Position, so we will
			//need to carry that extra movement forward after processing the logic that
			//occurs on the exact tile position.
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
			if (IsAutopilotEngaged && ((int)serverState.Position.x == (int)Target.x
									   || (int)serverState.Position.y == (int)Target.y))
			{
				StartCoroutine(TravelToTarget());
			}
			//now we can carry on with any excess movement we had discarded earlier, now
			//that we've already ran the logic that needs to happen on the exact tile position
			if (actualNewPosition != null)
			{
				serverState.Position = actualNewPosition.Value;
			}
		}
		else
		{
			//			Logger.LogTrace( "Stopping due to safety protocols!",Category.Shuttles );
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
			if (!MatrixManager.IsPassableAtAllMatrices(sensorPos, sensorPos + dir.RoundToInt(), isServer: true,
											collisionType: matrixColliderType, excludeList: excludeList))
			{
				Logger.LogTrace(
					$"Can't pass {serverTargetState.Position}->{serverTargetState.Position + dir} (because {sensorPos}->{sensorPos + dir})!",
					Category.Shuttles);
				return false;
			}
		}

		//		Logger.LogTrace( $"Passing {serverTargetState.Position}->{serverTargetState.Position+dir} ", Category.Shuttles );
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
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(localSensorAggrigateVector, matrixInfo, serverTargetState);

			// Exclude the rotating matrix, we shouldn't be able to collide with ourselves
			int[] excludeList = { matrixInfo.Id };
			if (!MatrixManager.IsPassableAtAllMatrices(sensorPos, sensorPos, isServer: true,
											collisionType: matrixColliderType, includingPlayers: true, excludeList: excludeList))
			{
				Logger.LogTrace(
					$"Can't rotate at {serverTargetState.Position}->{serverTargetState.Position } (because {sensorPos} is occupied)!",
					Category.Shuttles);
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
		Logger.LogTraceFormat("{0} setting client / client target state from message {1}", Category.Shuttles, this, newState);


		if (!Equals(oldState.FacingDirection, newState.FacingDirection))
		{
			if (!receivedInitialState && !pendingInitialRotation)
			{
				pendingInitialRotation = true;
			}
			inProgressRotation = oldState.FacingDirection.OffsetTo(newState.FacingDirection);
			Logger.LogTraceFormat("{0} starting rotation progress to {1}", Category.Shuttles, this, newState.FacingDirection);
			MatrixMoveEvents.OnRotate.Invoke(new MatrixRotationInfo(this, inProgressRotation.Value, NetworkSide.Client, RotationEvent.Start));
		}

		if (rcsModeActive)
		{
			// enable engines for clients that aren't using RCS
			if (clientState.IsMoving && !oldState.IsMoving && rcsMovementStartPosition != Vector2Int.RoundToInt(clientState.Position))
			{
				OrientationEnum releativeDirection = GetReleativeOrientation(clientState.FacingDirection.AsEnum(), clientState.FlyingDirection.AsEnum());
				StartCoroutine(ClientRCSCoroutine(releativeDirection, clientState.FlyingDirection, false));
			}

			// don't teleport player back to start point
			Vector2Int roundedPosition = Vector2Int.RoundToInt(clientState.Position);
			if (roundedPosition == rcsMovementStartPosition)
			{
				clientState.FlyingDirection = oldState.FlyingDirection;
				clientState.Position = oldState.Position;
				clientState.Speed = oldState.Speed;
				clientState.IsMoving = oldState.IsMoving;

				canClientUseRcs = true;
			}
			else if (roundedPosition == rcsMovementTargetPosition)
			{
				clientState.Position = new Vector3(rcsMovementTargetPosition.x, rcsMovementTargetPosition.y, clientState.Position.z);
				canClientUseRcs = true;
			}
			// else - player is teleported to server position
			else
			{
				canClientUseRcs = true;
			}
		}

		if (!oldState.IsMoving && newState.IsMoving)
		{
			MatrixMoveEvents.OnStartMovementClient.Invoke();
		}

		if (oldState.IsMoving && !newState.IsMoving)
		{
			MatrixMoveEvents.OnStopMovementClient.Invoke();
		}

		if ((int)oldState.Speed != (int)newState.Speed)
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
			Logger.LogTraceFormat("{0} setting server state from target state {1}", Category.Shuttles, this, serverState);
			NotifyPlayers();
		}
	}

	///  Currently sending to everybody, but should be sent to nearby players only
	[Server]
	private void NotifyPlayers()
	{
		//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)\
		if (!IsMovingServer || serverState.Inform)
		{
			serverState.RotationTime = rotTime;

			//fixme: this whole class behaves like ass!
			if (serverState.RotationTime != serverTargetState.RotationTime)
			{ //Doesn't guarantee that matrix will stop
				MatrixMoveMessage.SendToAll(gameObject, serverState);
			}
			else
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
			Logger.LogTraceFormat("{0} server target facing / flying {1}", Category.Shuttles, this, desiredOrientation);

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
		Logger.LogTraceFormat("{0} server target flying {1}", Category.Shuttles, this, newFlyingDirection);
	}

	/// Changes facing direction without changing flying direction, for use in reversing in EscapeShuttle
	[Server]
	public bool ChangeFacingDirection(Orientation newFacingDirection)
	{
		if (CanRotateTo(newFacingDirection))
		{
			serverTargetState.FacingDirection = newFacingDirection;
			Logger.LogTraceFormat("{0} server target facing  {1}", Category.Shuttles, this, newFacingDirection);

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
			int xProjectionX = (int)xProjection.x;
			int targetX = (int)Target.x;

			Vector3 yProjection = Vector3.Project(pos, Vector3.up);
			int yProjectionY = (int)yProjection.y;
			int targetY = (int)Target.y;

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

	/// <summary>
	/// [Server] start RCS movement on server side
	/// </summary>
	/// <seealso cref="RcsMoveClient(Orientation)"></seealso>
	/// <param name="orientation">flying direction in world space</param>
	[Server]
	public void RcsMoveServer(Orientation orientation)
	{
		if (!canServerUseRcs || serverState.IsMoving || IsForceStopped || (!IsFueled && RequiresFuel)) return;

		// get releative direcion based on shutter facing
		OrientationEnum releativeDirection = GetReleativeOrientation(serverState.FacingDirection.AsEnum(), orientation.AsEnum());

		// move only if shuttle has RCS thrusters pointing target direction
		if (rcsThrusters[releativeDirection].Count > 0)
		{
			StartCoroutine(ServerRCSCoroutine(serverState.FlyingDirection, serverState.Speed, moveLimit));
			RcsStartMovementServer(orientation);
		}
	}

	/// <summary>
	/// [Client] start RCS movement on client side (prediction)
	/// </summary>
	/// <seealso cref="RcsMoveServer(Orientation)"></seealso>
	/// <param name="orientation">flying direction in world space</param>
	[Client]
	public void RcsMoveClient(Orientation orientation)
	{
		if (!canClientUseRcs || clientState.IsMoving || IsForceStopped || (!IsFueled && RequiresFuel))
			return;

		// get releative direcion based on shutter facing
		OrientationEnum releativeDirection = GetReleativeOrientation(clientState.FacingDirection.AsEnum(), orientation.AsEnum());

		// move only if shuttle has RCS thrusters pointing target direction
		if (rcsThrusters[releativeDirection].Count > 0)
		{
			StartCoroutine(ClientRCSCoroutine(releativeDirection, clientState.FlyingDirection));
			RcsStartMovementClient(orientation);
		}
	}

	/// <summary>
	/// Get releative orientation based on shutter facing
	/// </summary>
	/// <param name="facingDirection">shuttle facing direction</param>
	/// <param name="worldOrientation">world space orientation</param>
	private OrientationEnum GetReleativeOrientation(OrientationEnum facingDirection, OrientationEnum worldOrientation)
	{
		switch (facingDirection)
		{
			case OrientationEnum.Up:
				switch (worldOrientation)
				{
					case OrientationEnum.Down:
						return OrientationEnum.Up;
					case OrientationEnum.Up:
						return OrientationEnum.Down;
				}
				break;
			case OrientationEnum.Right:
				switch (worldOrientation)
				{
					case OrientationEnum.Up:
						return OrientationEnum.Left;
					case OrientationEnum.Right:
						return OrientationEnum.Down;
					case OrientationEnum.Down:
						return OrientationEnum.Right;
					case OrientationEnum.Left:
						return OrientationEnum.Up;
				}
				break;
			case OrientationEnum.Down:
				switch (worldOrientation)
				{
					case OrientationEnum.Up:
						return OrientationEnum.Up;
					case OrientationEnum.Right:
						return OrientationEnum.Left;
					case OrientationEnum.Down:
						return OrientationEnum.Down;
					case OrientationEnum.Left:
						return OrientationEnum.Right;
				}
				break;
			case OrientationEnum.Left:
				switch (worldOrientation)
				{
					case OrientationEnum.Up:
						return OrientationEnum.Right;
					case OrientationEnum.Right:
						return OrientationEnum.Up;
					case OrientationEnum.Down:
						return OrientationEnum.Left;
					case OrientationEnum.Left:
						return OrientationEnum.Down;
				}
				break;
		}

		return worldOrientation;
	}

	/// <summary>
	/// Turns on RCS engines, waits for flyTime (1s by default) and turns off the engines.
	/// </summary>
	/// <param name="releativeDirection">RCS flight direction</param>
	/// <param name="defaultFlyingDirection">flight direction before turning on the RCS</param>
	/// <param name="flyTime">time for which the rcs engines are to be switched on</param>
	[Client]
	private IEnumerator ClientRCSCoroutine(OrientationEnum releativeDirection, Orientation defaultFlyingDirection, bool isControlligPlayer = true, float flyTime = 1f)
	{
		// loop trough all thrusters
		foreach (var item in rcsThrusters)
		{
			// enable particle if thruster is pointing in movement direction
			// else disable
			bool active = item.Key == releativeDirection;
			foreach (var thruster in item.Value)
			{
				thruster.thrusterParticles.gameObject.SetActive(active);
			}
		}

		if(isControlligPlayer)
			canClientUseRcs = false;

		yield return WaitFor.Seconds(flyTime);

		// set flyingDirection to movement direction after using RCS
		clientState.FlyingDirection = defaultFlyingDirection;
		clientState.IsMoving = false;

		// disable all thrusters
		foreach (var thruster in rcsThrusters[releativeDirection])
		{
			thruster.thrusterParticles.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Turns on RCS engines, waits for flyTime (1s by default) and turns off the engines.
	/// </summary>
	/// <param name="defaultFlyingDirection">flight direction before turning on the RCS</param>
	/// <param name="flyTime">time for which the rcs engines are to be switched on</param>
	[Server]
	private IEnumerator ServerRCSCoroutine(Orientation defaultFlyingDirection, float defaultSpeed, int defaultMoveLimit, float flyTime = 1)
	{
		canServerUseRcs = false;

		yield return WaitFor.Seconds(flyTime);

		canServerUseRcs = true;

		serverTargetState.FlyingDirection = defaultFlyingDirection;
		serverTargetState.Speed = defaultSpeed;
		serverTargetState.IsMoving = false;

		moveLimit = defaultMoveLimit;

		MatrixMoveEvents.OnStopMovementServer.Invoke();
		RequestNotify();
	}

	/// <summary>
	/// Search matrix for RCS thrusters and cache them
	/// </summary>
	public void CacheRcs()
	{
		ClearRcsCache();

		foreach (Transform child in matrixInfo.Objects)
		{
			if (child.CompareTag("Rcs") && child.TryGetComponent(out RcsThruster thruster))
			{
				// remove all listeners
				if (thruster.OnThrusterDestroyedEvent != null)
					foreach (Delegate listener in thruster.OnThrusterDestroyedEvent.GetInvocationList())
					{
						thruster.OnThrusterDestroyedEvent -= (RcsThruster.OnThrusterDestroyedDelegate)listener;
					}

				// add listener to remove thruster from list when destroyed
				thruster.OnThrusterDestroyedEvent += () => rcsThrusters[thruster.directional.MappedOrientation].Remove(thruster);
				// add thruster to list
				rcsThrusters[thruster.directional.MappedOrientation].Add(thruster);
			}
		}
	}

	/// <summary>
	/// Clear cached RCS thrusters
	/// </summary>
	void ClearRcsCache()
	{
		rcsThrusters[OrientationEnum.Up].Clear();
		rcsThrusters[OrientationEnum.Right].Clear();
		rcsThrusters[OrientationEnum.Down].Clear();
		rcsThrusters[OrientationEnum.Left].Clear();
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
		if (!Application.isPlaying)
		{ //Showing matrix pivot if game is stopped
			Gizmos.color = color1.WithAlpha(0.6f);
			Gizmos.DrawCube(transform.position, Vector3.one);
			Gizmos.color = color1;
			Gizmos.DrawWireCube(transform.position, Vector3.one);

			DebugGizmoUtils.DrawArrow(transform.position, clientState.FlyingDirection.Vector * 2);
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
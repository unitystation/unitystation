using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Matrix Move Server
/// </summary>
public partial class MatrixMove
{
	//server-only values
	public MatrixState ServerState => serverState;
	public bool IsMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool IsRotatingServer => NeedsRotationClient; //todo: calculate rotation time on server instead
	///used for syncing with players, matters only for server
	private MatrixState serverState = MatrixState.Invalid;
	/// future state that collects all changes
	private MatrixState serverTargetState = MatrixState.Invalid;
	//Autopilot target
	private Vector3 Target = TransformState.HiddenPos;
	///Zero means 100% accurate, but will lead to peculiar behaviour (autopilot not reacting fast enough on high speed -> going back/in circles etc)
	private int AccuracyThreshold = 1;

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
	public void ToggleRcs(bool on, ConnectedPlayer subject, uint consoleId)
	{
		rcsModeActive = on;
		if (on)
		{
			if (subject != null)
			{
				ToggleRcsPlayerControl.UpdateClient(subject, consoleId, true);
				CacheRcs();
				playerControllingRcs = subject;
			}
		}
		else
		{
			if (playerControllingRcs != null)
			{
				ToggleRcsPlayerControl.UpdateClient(playerControllingRcs, consoleId, false);
				playerControllingRcs = null;
			}
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
		//Not allowing movement without any thrusters:
		if (HasWorkingThrusters && (IsFueled || !RequiresFuel))
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
			if (IsAutopilotEngaged && ((int) serverState.Position.x == (int) Target.x
			                           || (int) serverState.Position.y == (int) Target.y))
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
//			Logger.LogTrace( "Stopping due to safety protocols!",Category.Matrix );
			StopMovement();
			TryNotifyPlayers();
		}
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
			serverState.Inform = true;
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

	/// Makes matrix start moving towards given world pos
	[Server]
	public void AutopilotTo(Vector2 position)
	{
		Target = position;
		StartCoroutine(TravelToTarget());
	}

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

	[Server]
	public void ProcessRcsMoveRequest(ConnectedPlayer sentBy, Vector2Int dir)
	{
		if (sentBy == playerControllingRcs)
		{
			serverState.Position += dir.To3Int();
			serverTargetState.Position += dir.To3Int();
			serverState.Inform = true;
			serverTargetState.Inform = true;
			NotifyPlayers();
		}
	}
}

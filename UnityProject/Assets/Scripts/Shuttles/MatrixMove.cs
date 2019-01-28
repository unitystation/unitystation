using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Encapsulates the state of a matrix's motion / facing
/// </summary>
public struct MatrixState
{
	[NonSerialized] public bool Inform;
	public bool IsMoving;
	public float Speed;
	public Orientation Direction; //Direction of movement
	public int RotationTime; //in frames?

	public Vector3 Position;

	/// <summary>
	/// Current absolute orientation
	/// </summary>
	public Orientation orientation;
	/// <summary>
	/// Initial absolute orientation as per editor placement. Defaults to up, otherwise uses initial flying direction
	/// </summary>
	public Orientation initialOrientation;
	/// <summary>
	/// Offset from initial orientation (as per editor placement) to orientation
	/// </summary>
	public RotationOffset RotationOffset => initialOrientation.OffsetTo(orientation);

	public static readonly MatrixState Invalid = new MatrixState {Position = TransformState.HiddenPos};

	public bool Equals(MatrixState other)
	{
		return Position.Equals(other.Position) && orientation.Equals(other.orientation);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
		{
			return false;
		}

		return obj is MatrixState && Equals((MatrixState) obj);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			return (Position.GetHashCode() * 397) ^ orientation.GetHashCode();
		}
	}

	public override string ToString()
	{
		return $"{nameof(Inform)}: {Inform}, {nameof(IsMoving)}: {IsMoving}, {nameof(Speed)}: {Speed}, " +
		       $"{nameof(Direction)}: {Direction}, {nameof(Position)}: {Position}, {nameof(orientation)}: {orientation}, {nameof(RotationTime)}: {RotationTime}";
	}
}

/// <summary>
/// Behavior which allows an entire matrix to move and rotate (and be synced over the network)
/// </summary>
public class MatrixMove : ManagedNetworkBehaviour
{
	public bool IsMoving => isMovingClient; //?

	//server-only values
	public MatrixState State => serverState;

	///used for syncing with players, matters only for server
	private MatrixState serverState = MatrixState.Invalid;

	/// future state that collects all changes
	private MatrixState serverTargetState = MatrixState.Invalid;

	public bool SafetyProtocolsOn { get; set; } = true;
	private bool isMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool isRotatingServer => IsRotatingClient; //todo: calculate rotation time on server instead
	private bool isAutopilotEngaged => Target != TransformState.HiddenPos;

	//client-only values
	public MatrixState ClientState => clientState;

	///client's transform, can get dirty/predictive
	private MatrixState clientState = MatrixState.Invalid;

	public bool StateInit { get; private set; } = false;

	/// Is only present to match server's flight routines
	private MatrixState clientTargetState = MatrixState.Invalid;

	private bool isMovingClient => clientState.IsMoving && clientState.Speed > 0f;

	/// <summary>
	/// Does current transform rotation not yet match the target client offset?
	/// </summary>
	public bool IsRotatingClient =>
		Quaternion.Angle(transform.rotation, clientState.RotationOffset.Quaternion) != 0;

	private bool ClientPositionsMatch => clientTargetState.Position == clientState.Position;

	//editor (global) values
	public UnityEvent OnStart = new UnityEvent();
	public UnityEvent OnStop = new UnityEvent();
	public OrientationEvent OnRotate = new OrientationEvent();
	public DualFloatEvent OnSpeedChange = new DualFloatEvent();

	/// Initial flying direction from editor
	public Vector2 flyingDirection = Vector2.up;

	/// max flying speed from editor
	public float maxSpeed = 20f;

	private readonly int rotTime = 90;
	public KeyCode startKey = KeyCode.G;
	public KeyCode leftKey = KeyCode.Keypad4;
	public KeyCode rightKey = KeyCode.Keypad6;

	///initial pos for offset calculation
	[HideInInspector]
	public Vector3Int InitialPos;
	[SyncVar(hook = nameof(UpdateInitialPos))] private Vector3 initialPosition;
	private void UpdateInitialPos(Vector3 sync)
	{
		InitialPos = sync.RoundToInt();
	}

	/// local pivot point
	[HideInInspector]
	public Vector3Int Pivot;
	[SyncVar(hook = nameof(UpdatePivot))] private Vector3 pivot;
	private void UpdatePivot(Vector3 sync)
	{
		Pivot = sync.RoundToInt();
	}

	private Vector3Int[] SensorPositions;

	public MatrixInfo MatrixInfo;

	private Vector3 mPreviousPosition;
	private Vector2 mPreviousFilteredPosition;
	private bool monitorOnRot = false;

	private Vector3 clampedPosition
	{
		set
		{
			transform.position =
				LightingSystem.GetPixelPerfectPosition(value, mPreviousPosition, mPreviousFilteredPosition);

			mPreviousPosition = value;
			mPreviousFilteredPosition = transform.position;
		}
	}

	public override void OnStartServer()
	{
		InitServerState();
		base.OnStartServer();
		NotifyPlayers();
	}

	[Server]
	private void InitServerState()
	{
		if (flyingDirection == Vector2.zero)
		{
			Logger.LogWarning($"{gameObject.name} move direction unclear", Category.Matrix);
			serverState.Direction = Orientation.Up;
		}
		else
		{
			serverState.Direction = Orientation.From(Vector2Int.RoundToInt(flyingDirection));
		}

		Vector3Int initialPositionInt =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		initialPosition = initialPositionInt;
		InitialPos = initialPosition.RoundToInt();
		//initialOrientation = Orientation.From( serverState.Direction );
		serverState.initialOrientation = serverState.Direction;

		var child = transform.GetChild( 0 );
		MatrixInfo = MatrixManager.Get( child.gameObject );
		var childPosition = Vector3Int.CeilToInt(new Vector3(child.transform.position.x, child.transform.position.y, 0));
		pivot =  initialPosition - childPosition;
		Pivot = pivot.RoundToInt();

		Logger.LogTraceFormat("{0}: pivot={1} initialPos={2}, initialOrientation={3}", Category.Matrix, gameObject.name,
			pivot, initialPositionInt, serverState.initialOrientation);
		serverState.Speed = 1f;
		serverState.Position = initialPosition;
		serverState.orientation = serverState.initialOrientation;
		serverTargetState = serverState;

		clientState = serverState;
		clientTargetState = serverState;
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
			           $" direction is {State.Direction}", Category.Matrix);
		}
	}

	///managed by UpdateManager
	public override void UpdateMe()
	{
		if (isServer)
		{
//			if ( Input.GetKeyDown( startKey ) ) {
//				ToggleMovement();
//			}
//			if ( Input.GetKeyDown( KeyCode.KeypadPlus ) ) {
//				AdjustSpeed( 1 );
//			}
//			if ( Input.GetKeyDown( KeyCode.KeypadMinus ) ) {
//				AdjustSpeed( -1 );
//			}
//			if ( Input.GetKeyDown( leftKey ) ) {
//				TryRotate( false );
//			}
//			if ( Input.GetKeyDown( rightKey ) ) {
//				TryRotate( true );
//			}
			CheckMovementServer();
		}

		CheckMovement();
	}

	[Server]
	public void ToggleMovement()
	{
		if (isMovingServer)
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
		//Setting speed if there is none
		if (serverTargetState.Speed <= 0)
		{
			SetSpeed(1);
		}

//		Logger.Log($"Started moving with speed {serverTargetState.Speed}");
		serverTargetState.IsMoving = true;
		RequestNotify();
	}

	/// Stop movement
	[Server]
	public void StopMovement()
	{
//		Logger.Log("Stopped movement");
		serverTargetState.IsMoving = false;
		//To stop autopilot
		DisableAutopilotTarget();
	}

	public int MoveCur = -1;
	public int MoveLimit = -1;

	/// Move for n tiles, regardless of direction, and stop
	[Server]
	public void MoveFor(int tiles)
	{
		if (tiles < 1)
		{
			tiles = 1;
		}

		if (!isMovingServer)
		{
			StartMovement();
		}

		MoveCur = 0;
		MoveLimit = tiles;
	}

	/// Checks if it still can move according to MoveFor limits.
	/// If true, increment move count
	[Server]
	public bool CanMoveFor()
	{
		if (MoveCur == MoveLimit && MoveCur != -1)
		{
			MoveCur = -1;
			MoveLimit = -1;
			return false;
		}

		MoveCur++;
		return true;
	}

	/// Changes moving direction, for use in reversing in EscapeShuttle.cs
	[Server]
	public void ChangeDir(Orientation newdir)
	{
		serverTargetState.Direction = newdir;
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

		if (absoluteValue > maxSpeed)
		{
			Logger.LogWarning($"MaxSpeed {maxSpeed} reached, not going further", Category.Matrix);
			if (serverTargetState.Speed >= maxSpeed)
			{
				//Not notifying people if some dick is spamming "increase speed" button at max speed
				return;
			}

			serverTargetState.Speed = maxSpeed;
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

	/// Clientside movement routine
	private void CheckMovement()
	{
		if (Equals(clientState, MatrixState.Invalid))
		{
			return;
		}

		if (IsRotatingClient)
		{
			RotationOffset targetOffset = clientState.RotationOffset;
			bool needsRotation = clientState.RotationTime != 0 &&
			                     !Mathf.Approximately(Quaternion.Angle(transform.rotation, targetOffset.Quaternion), 0);
			if (needsRotation)
			{
				transform.rotation =
					Quaternion.RotateTowards(transform.rotation,
						targetOffset.Quaternion,
						Time.deltaTime * clientState.RotationTime);
			}
			else
			{
				// Finishes the job of Lerp and straightens the ship with exact angle value
				transform.rotation = targetOffset.Quaternion;
			}
		}
		else if (isMovingClient)
		{
			//Only move target if rotation is finished
			SimulateStateMovement();
		}

		if (!IsRotatingClient && monitorOnRot)
		{
			monitorOnRot = false;
			//This is ok for occasional state changes like end of rot:
			gameObject.BroadcastMessage("MatrixMoveStopRotation", null, SendMessageOptions.DontRequireReceiver);
		}

		//Lerp
		if (clientState.Position != transform.position)
		{
			float distance = Vector3.Distance(clientState.Position, transform.position);
			bool shouldWarp = distance > 2 || IsRotatingClient;

			//Teleport (Greater then 30 unity meters away from server target):
			if (distance > 30f)
			{
				clampedPosition = clientState.Position;
				return;
			}

			//FIXME Remove this once lerping has been properly fixed with Pixel Perfect movement:
			//If stopped then lerp to target
			if (!clientState.IsMoving && distance > 0f)
			{
				transform.position = Vector3.MoveTowards(transform.position, clientState.Position,
					clientState.Speed * Time.deltaTime * (shouldWarp ? (distance * 2) : 1));
				mPreviousPosition = transform.position;
				mPreviousFilteredPosition = transform.position;
				return;
			}

			//FIXME: We need to use MoveTowards or some other lerp function as ClientState is like server waypoints and does not contain lerp positions
			//FIXME: Currently shuttles teleport to each position received via server instead of lerping towards them
			clampedPosition = clientState.Position;

			// Activate warp speed if object gets too far away or have to rotate
			//Vector3.MoveTowards( transform.position, clientState.Position, clientState.Speed * Time.deltaTime * ( shouldWarp ? (distance * 2) : 1 ) );
		}
	}

	/// Serverside movement routine
	[Server]
	private void CheckMovementServer()
	{
		//Not doing any serverside movement while rotating
		if (isRotatingServer)
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
		if (!isMovingServer || isGonnaStop || !ServerPositionsMatch)
		{
			return;
		}

		if (CanMoveFor() && (!SafetyProtocolsOn || CanMoveTo(serverTargetState.Direction)))
		{
			var goal = Vector3Int.RoundToInt(serverState.Position + serverTargetState.Direction.Vector);
			//keep moving
			serverTargetState.Position = goal;
			if (isAutopilotEngaged && ((int) serverState.Position.x == (int) Target.x
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
			Vector3Int sensorPos = MatrixManager.LocalToWorldInt(sensor, MatrixInfo, serverTargetState);
			if (!MatrixManager.IsPassableAt(sensorPos, sensorPos + dir.RoundToInt()))
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

	/// Manually set matrix to a specific position.
	[Server]
	public void SetPosition(Vector3 pos, bool notify = true)
	{
		Vector3Int intPos = Vector3Int.RoundToInt(pos);
		serverState.Position = intPos;
		serverTargetState.Position = intPos;
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

		if (!Equals(oldState.orientation, newState.orientation))
		{
			if (!StateInit)
			{
				//this is the first state, so set rotation based on offset from initial position
				OnRotate.Invoke(newState.RotationOffset);
			}
			else
			{
				//update based on offset from old state
				OnRotate.Invoke(oldState.orientation.OffsetTo(newState.orientation));
			}

			//This is ok for occasional state changes like beginning of rot:
			gameObject.BroadcastMessage("MatrixMoveStartRotation", null, SendMessageOptions.DontRequireReceiver);
			monitorOnRot = true;
		}

		if (!StateInit)
		{
			StateInit = true;
		}

		if (!oldState.IsMoving && newState.IsMoving)
		{
			OnStart.Invoke();
		}

		if (oldState.IsMoving && !newState.IsMoving)
		{
			OnStop.Invoke();
		}

		if ((int) oldState.Speed != (int) newState.Speed)
		{
			OnSpeedChange.Invoke(oldState.Speed, newState.Speed);
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

		if (isMovingClient && !IsRotatingClient)
		{
			Vector3Int goal = Vector3Int.RoundToInt(clientState.Position + clientTargetState.Direction.Vector);
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
		if (!isMovingServer || serverState.Inform)
		{
			serverState.RotationTime = rotTime;

			MatrixMoveMessage.SendToAll(gameObject, serverState);
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
		if (!isRotatingServer)
		{
			Rotate(clockwise);
		}
	}

	/// Imperative rotate left or right
	[Server]
	public void Rotate(bool clockwise)
	{
		RotateTo(serverTargetState.orientation.Rotate(clockwise ? 1 : -1));
	}

	/// Imperative rotate to desired orientation
	[Server]
	public void RotateTo(Orientation desiredOrientation)
	{
		serverTargetState.orientation = desiredOrientation;
		Logger.LogTraceFormat("Orientation is now {0}, Corrected direction from {1} to {2}", Category.Matrix,
			serverTargetState.orientation, serverTargetState.Direction, desiredOrientation);
		serverTargetState.Direction = desiredOrientation;
		RequestNotify();
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
		if (isAutopilotEngaged)
		{
			var pos = serverState.Position;
			if (Vector3.Distance(pos, Target) <= AccuracyThreshold)
			{
				StopMovement();
				yield break;
			}

			Orientation currentDir = serverState.orientation;

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
					RotateTo(xRotationsTo < yRotationsTo ? xDesiredDir : yDesiredDir);
					//wait till it rotates
					yield return YieldHelper.Second;
				}
			}

			if (!serverState.IsMoving)
			{
				StartMovement();
			}

			//Relaunching self once in a while as CheckMovementServer check can fail in rare occasions
			yield return YieldHelper.Second;
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
		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = serverState.Position;
		Gizmos.DrawWireCube(serverPos, size1);
		if (serverState.IsMoving)
		{
			GizmoUtils.DrawArrow(serverPos + Vector3.right / 3, serverState.Direction.Vector * serverState.Speed);
			GizmoUtils.DrawText(serverState.Speed.ToString(), serverPos + Vector3.right, 15);
		}

		//serverTargetState
		Gizmos.color = color2;
		Vector3 serverTargetPos = serverTargetState.Position;
		Gizmos.DrawWireCube(serverTargetPos, size2);
		if (serverTargetState.IsMoving)
		{
			GizmoUtils.DrawArrow(serverTargetPos, serverTargetState.Direction.Vector * serverTargetState.Speed);
			GizmoUtils.DrawText(serverTargetState.Speed.ToString(), serverTargetPos + Vector3.down, 15);
		}

		//clientState
		Gizmos.color = color3;
		Vector3 pos = clientState.Position;
		Gizmos.DrawWireCube(pos, size3);
		if (clientState.IsMoving)
		{
			GizmoUtils.DrawArrow(pos + Vector3.left / 3, clientState.Direction.Vector * clientState.Speed);
			GizmoUtils.DrawText(clientState.Speed.ToString(), pos + Vector3.left, 15);
		}
	}
#endif
}

/// <summary>
/// Event sent when rotation offset of the receiver should change. RotationOffset indicates how much
/// the object should rotate from their CURRENT direction. I.E. if object recieves RotationOffset.Left and
/// is currently facing Right, they should now face Up (since Up is a left turn from Right).
/// </summary>
[Serializable]
public class OrientationEvent : UnityEvent<RotationOffset>
{
}

[Serializable]
public class DualFloatEvent : UnityEvent<float,float> {}
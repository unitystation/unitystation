using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public struct MatrixState
{
	[NonSerialized]
	public bool Inform;
	public bool IsMoving;
	public float Speed;
	public Vector2 Direction; //Direction of movement
	public int RotationTime; //in frames?
	public Vector3 Position;
	/// Matrix rotation. Default is upright (Orientation.Up)
	public Orientation Orientation;

	public static readonly MatrixState Invalid = new MatrixState{Position = TransformState.HiddenPos};

	public override string ToString() {
		return $"{nameof( Inform )}: {Inform}, {nameof( IsMoving )}: {IsMoving}, {nameof( Speed )}: {Speed}, " +
		       $"{nameof( Direction )}: {Direction}, {nameof( Position )}: {Position}, {nameof( Orientation )}: {Orientation}, {nameof( RotationTime )}: {RotationTime}";
	}
}

public class MatrixMove : ManagedNetworkBehaviour {
	public bool IsMoving => isMovingServer;
	
	//server-only values
	public MatrixState State => serverState;
	///used for syncing with players, matters only for server
	private MatrixState serverState = MatrixState.Invalid;
	/// future state that collects all changes
	private MatrixState serverTargetState = MatrixState.Invalid; 
	public bool SafetyProtocolsOn { get; set; }
	private bool isMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool isRotatingServer => IsRotatingClient; //todo: calculate rotation time on server instead
	private bool isAutopilotEngaged => Target != TransformState.HiddenPos;
	
	//client-only values
	public MatrixState ClientState => clientState;
	///client's transform, can get dirty/predictive
	private MatrixState clientState = MatrixState.Invalid; 
	/// Is only present to match server's flight routines 
	private MatrixState clientTargetState = MatrixState.Invalid; 
	private bool isMovingClient => clientState.IsMoving && clientState.Speed > 0f;
	public bool IsRotatingClient => transform.rotation.eulerAngles.z != clientState.Orientation.Degree;
	private bool ClientPositionsMatch => clientTargetState.Position == clientState.Position;
	
	//editor (global) values
	public UnityEvent OnStart;
	public UnityEvent OnStop;
	public OrientationEvent OnRotate;
	public DualFloatEvent OnSpeedChange;
	
	/// Initial flying direction from editor
	public Vector2 flyingDirection = Vector2.up;
	/// max flying speed from editor
	public float maxSpeed = 20f;
	private readonly int rotTime = 90;
	public KeyCode startKey = KeyCode.G;
	public KeyCode leftKey = KeyCode.Keypad4;
	public KeyCode rightKey = KeyCode.Keypad6;
	///initial pos for offset calculation
	public Vector3Int InitialPos => Vector3Int.RoundToInt(initialPosition);
	[SyncVar] private Vector3 initialPosition;
	/// local pivot point
	public Vector3Int Pivot => Vector3Int.RoundToInt(pivot);
	[SyncVar] private Vector3 pivot;

	public override void OnStartServer()
	{
		InitServerState();
		base.OnStartServer();
		NotifyPlayers();
	}

	[Server]
	private void InitServerState()
	{
		if ( flyingDirection == Vector2.zero ) {
			Debug.LogWarning($"{gameObject.name} move direction unclear");
			serverState.Direction = Vector2.up;
		} else {
			serverState.Direction = Vector2Int.RoundToInt(flyingDirection);
		}
		initialPosition = Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		var child = transform.GetChild( 0 );
		var childPosition = Vector3Int.CeilToInt(new Vector3(child.transform.position.x, child.transform.position.y, 0));
		pivot =  initialPosition - childPosition;
//		Debug.Log( $"Calculated pivot {pivot} for {gameObject.name}" );
		
		serverState.Speed = 1f;
		serverState.Position = initialPosition;
		serverState.Orientation = Orientation.Up;
		serverTargetState = serverState;

		clientState = serverState;
		clientTargetState = serverState;
	}

	///managed by UpdateManager
	public override void UpdateMe(){
		if ( isServer ) {
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
	public void ToggleMovement() {
		if ( isMovingServer ) {
			StopMovement();
		} else {
			StartMovement();
		}
	}

	/// Start moving. If speed was zero, it'll be set to 1
	[Server]
	public void StartMovement() {
		//Setting speed if there is none
		if ( serverTargetState.Speed <= 0 ) {
			SetSpeed( 1 );
		}
//		Debug.Log($"Started moving with speed {serverTargetState.Speed}");
		serverTargetState.IsMoving = true;
		RequestNotify();
	}
	/// Stop movement
	[Server]
	public void StopMovement() {
//		Debug.Log("Stopped movement");
		serverTargetState.IsMoving = false;
		//To stop autopilot
		DisableAutopilotTarget();
	}

	/// Call to stop chasing target
	[Server]
	public void DisableAutopilotTarget() {
		Target = TransformState.HiddenPos;
	}

	/// Adjust current ship's speed with a relative value
	[Server]
	public void AdjustSpeed( float relativeValue ) {
		float absSpeed = serverTargetState.Speed + relativeValue;
		SetSpeed( absSpeed );
	}

	/// Set ship's speed using absolute value. it will be truncated if it's out of bounds
	[Server]
	public void SetSpeed( float absoluteValue ) {
		if ( absoluteValue <= 0 ) {
			//Stop movement if speed is zero or below
			serverTargetState.Speed = 0;
			if ( serverTargetState.IsMoving ) {
				StopMovement();
			}
			return;
		}
		if ( absoluteValue > maxSpeed ) {
			Debug.LogWarning($"MaxSpeed {maxSpeed} reached, not going further");
			if ( serverTargetState.Speed >= maxSpeed ) {
				//Not notifying people if some dick is spamming "increase speed" button at max speed
				return;
			}
			serverTargetState.Speed = maxSpeed;
		} else {
			serverTargetState.Speed = absoluteValue;
		}
		//do not send speed updates when not moving
		if ( serverTargetState.IsMoving ) { 
			RequestNotify();
		}
	}
	/// Clientside movement routine
	private void CheckMovement()
	{
		if ( Equals( clientState, MatrixState.Invalid ) ) {
			return;
		}
		if ( IsRotatingClient ) {
			bool needsRotation = clientState.RotationTime != 0 && !Mathf.Approximately( transform.rotation.eulerAngles.z, clientState.Orientation.Degree );
			if ( needsRotation ) {
				transform.rotation =
					Quaternion.RotateTowards( transform.rotation, Quaternion.Euler( 0, 0, clientState.Orientation.Degree ),
						Time.deltaTime * clientState.RotationTime );
			} else {
				// Finishes the job of Lerp and straightens the ship with exact angle value
				transform.rotation = Quaternion.Euler( 0, 0, clientState.Orientation.Degree );
			}
		} else if ( isMovingClient ) {
			//Only move target if rotation is finished
			SimulateStateMovement();
		}
		
		//Lerp
		if ( clientState.Position != transform.position ) {
			float distance = Vector3.Distance( clientState.Position, transform.position );
			
//			Just set pos without any lerping if distance is too long (serverside teleportation assumed)
			bool shouldTeleport = distance > 30;
			if ( shouldTeleport ) {
				transform.position = clientState.Position;
				return;
			}
//			Activate warp speed if object gets too far away or have to rotate
			bool shouldWarp = distance > 2 || IsRotatingClient;
			transform.position =
				Vector3.MoveTowards( transform.position, clientState.Position, clientState.Speed * Time.deltaTime * ( shouldWarp ? (distance * 2) : 1 ) );		
		}
	}
	
	/// Serverside movement routine
	[Server]
	private void CheckMovementServer()
	{
		//Not doing any serverside movement while rotating
		if ( isRotatingServer ) {
			return;
		}
		//ServerState lerping to its target tile
		if ( !ServerPositionsMatch ) {
			serverState.Position =
				Vector3.MoveTowards( serverState.Position,
					serverTargetState.Position,
					serverState.Speed * Time.deltaTime );
			TryNotifyPlayers();
		}
		if ( isMovingServer ) {
			Vector3Int goal = Vector3Int.RoundToInt( serverState.Position + ( Vector3 ) serverTargetState.Direction );
			if ( !SafetyProtocolsOn || CanMoveTo( goal ) ) {
				//keep moving
				if ( ServerPositionsMatch ) 
				{
					serverTargetState.Position = goal;
					if ( isAutopilotEngaged && ( (int)serverState.Position.x == (int)Target.x 
											  || (int)serverState.Position.y == (int)Target.y ) ) {
						StartCoroutine( TravelToTarget() );
					}
				}
			} else {
				Debug.Log( "Stopping due to safety protocols!" );
				StopMovement();
			}
		}
	}


	private bool CanMoveTo(Vector3Int goal)
	{
		//todo: safety protocols
		return true;
	}

	/// Manually set matrix to a specific position.
	[Server]
	public void SetPosition( Vector3 pos, bool notify = true ) {
		Vector3Int intPos = Vector3Int.RoundToInt( pos );
		serverState.Position = intPos;
		serverTargetState.Position = intPos;
		if (notify) {
			NotifyPlayers();
		}
	}

	/// Called when MatrixMoveMessage is received
	public void UpdateClientState( MatrixState newState )
	{
		if ( !Equals(clientState.Orientation, newState.Orientation) ) {
			OnRotate.Invoke(clientState.Orientation, newState.Orientation);
		}
		if ( !clientState.IsMoving && newState.IsMoving ) {
			OnStart.Invoke();
		}
		if ( clientState.IsMoving && !newState.IsMoving ) {
			OnStop.Invoke();
		}
		if ( (int)clientState.Speed != (int)newState.Speed ) {
			OnSpeedChange.Invoke(clientState.Speed, newState.Speed);
		}
		clientState = newState;
		clientTargetState = newState;
	}

	///predictive perpetual flying
	private void SimulateStateMovement()
	{
		//ClientState lerping to its target tile
		if ( !ClientPositionsMatch ) {
			clientState.Position =
				Vector3.MoveTowards( clientState.Position,
					clientTargetState.Position,
					clientState.Speed * Time.deltaTime );
		}
		if ( isMovingClient && !IsRotatingClient ) {
			Vector3Int goal = Vector3Int.RoundToInt( clientState.Position + ( Vector3 ) clientTargetState.Direction );
				//keep moving
				if ( ClientPositionsMatch ) {
					clientTargetState.Position = goal;
				}
		}
	}

	/// Schedule notification for the next ServerPositionsMatch
	/// And check if it's able to send right now
	[Server]
	private void RequestNotify() {
		serverTargetState.Inform = true;
		TryNotifyPlayers();
	}

	///	Inform players when on integer position 
	[Server]
	private void TryNotifyPlayers() {
		if ( ServerPositionsMatch ) {
//				When serverState reaches its planned destination,
//				embrace all other updates like changed speed and rotation
			serverState = serverTargetState;
			NotifyPlayers();
		}
	}

	///  Currently sending to everybody, but should be sent to nearby players only
	[Server]
	private void NotifyPlayers() {
		//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
		if ( !isMovingServer || serverState.Inform ) 
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
	public void NotifyPlayer( GameObject playerGameObject, bool rotateImmediate = false ) {
		serverState.RotationTime = rotateImmediate ? 0 : rotTime;
		MatrixMoveMessage.Send(playerGameObject, gameObject, serverState);
	}

	///Only change orientation if rotation is finished
	[Server]
	public void TryRotate( bool clockwise ) {
		if ( !isRotatingServer ) {
			Rotate(clockwise);
		}
	}
	/// Imperative rotate left or right
	[Server]
	public void Rotate( bool clockwise )
	{
		RotateTo( clockwise ? serverTargetState.Orientation.Next() : serverTargetState.Orientation.Previous() );
	}
	
	/// Imperative rotate to desired orientation
	[Server]
	public void RotateTo( Orientation desiredOrientation ) 
	{
		var angleBetween = Orientation.DegreeBetween( serverTargetState.Orientation, desiredOrientation );
	
		serverTargetState.Orientation = desiredOrientation;
		
		//Correcting direction
		Vector3 newDirection = Quaternion.Euler( 0, 0, angleBetween ) * serverTargetState.Direction;
//		Debug.Log($"Orientation is now {serverTargetState.Orientation}, Corrected direction from {serverTargetState.Direction} to {newDirection}");
		serverTargetState.Direction = newDirection;
		RequestNotify();
	}

	private Vector3 Target = TransformState.HiddenPos;
		
	/// Makes matrix start moving towards given world pos
	[Server]
	public void AutopilotTo( Vector2 position ) {
		Target = position;
		StartCoroutine( TravelToTarget() );
	}

	///Zero means 100% accurate, but will lead to peculiar behaviour (autopilot not reacting fast enough on high speed -> going back/in circles etc)
	private static readonly int AccuracyThreshold = 1; 

	private IEnumerator TravelToTarget() {
		if ( isAutopilotEngaged ) 
		{
			var pos = serverState.Position;
			if ( Vector3.Distance(pos, Target) <= AccuracyThreshold ) {
				StopMovement();
				yield break;
			}
			Orientation currentDir = serverState.Orientation;
			
			Vector3 xProjection = Vector3.Project( pos, Vector3.right );
			int xProjectionX = (int) xProjection.x;
			int targetX = (int) Target.x;
			
			Vector3 yProjection = Vector3.Project( pos, Vector3.up );
			int yProjectionY = (int) yProjection.y;
			int targetY = (int) Target.y;

			bool xNeedsChange = Mathf.Abs(xProjectionX - targetX) > AccuracyThreshold;
			bool yNeedsChange = Mathf.Abs(yProjectionY - targetY) > AccuracyThreshold;

			Orientation xDesiredDir = ( targetX - xProjectionX ) > 0 ? Orientation.Left : Orientation.Right;
			Orientation yDesiredDir = ( targetY - yProjectionY ) > 0 ? Orientation.Up : Orientation.Down;

			if ( xNeedsChange || yNeedsChange ) 
			{
				int xDegreeTo = xNeedsChange ? Mathf.Abs( Orientation.DegreeBetween( currentDir, xDesiredDir ) ) : int.MaxValue;
				int yDegreeTo = yNeedsChange ? Mathf.Abs( Orientation.DegreeBetween( currentDir, yDesiredDir ) ) : int.MaxValue;
				
				//don't rotate if it's not needed
				if ( xDegreeTo != 0 && yDegreeTo != 0 ) {
					//if both need change determine faster rotation first
					RotateTo( xDegreeTo < yDegreeTo ? xDesiredDir : yDesiredDir );
					//wait till it rotates
					yield return YieldHelper.Second;
				}
			} 

			if ( !serverState.IsMoving ) {
				StartMovement();
			}
			//Relaunching self once in a while as CheckMovementServer check can fail in rare occasions 
			yield return YieldHelper.Second;
			StartCoroutine( TravelToTarget() );
		}
		yield return null;
	}

#if UNITY_EDITOR
	//Visual debug
	private Vector3 size1 = Vector3.one;
	private Vector3 size2 = new Vector3( 0.9f, 0.9f, 0.9f );
	private Vector3 size3 = new Vector3( 0.8f, 0.8f, 0.8f );
	private Color color1 = Color.red;
	private Color color2 = DebugTools.HexToColor( "81a2c7" );
	private Color color3 = Color.white;

	private void OnDrawGizmos() {
		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = serverState.Position;
		Gizmos.DrawWireCube( serverPos, size1 );
		if ( serverState.IsMoving ) {
			GizmoUtils.DrawArrow( serverPos + Vector3.right / 3, serverState.Direction * serverState.Speed );
			GizmoUtils.DrawText( serverState.Speed.ToString(), serverPos + Vector3.right, 15 );
		}
		//serverTargetState
		Gizmos.color = color2;
		Vector3 serverTargetPos = serverTargetState.Position;
		Gizmos.DrawWireCube( serverTargetPos, size2 );
		if ( serverTargetState.IsMoving ) {
			GizmoUtils.DrawArrow( serverTargetPos, serverTargetState.Direction * serverTargetState.Speed );
			GizmoUtils.DrawText( serverTargetState.Speed.ToString(), serverTargetPos + Vector3.down, 15 );
		}
		
		//clientState
		Gizmos.color = color3;
		Vector3 pos = clientState.Position;
		Gizmos.DrawWireCube( pos, size3 );
		if ( clientState.IsMoving ) {
			GizmoUtils.DrawArrow( pos + Vector3.left / 3, clientState.Direction * clientState.Speed );
			GizmoUtils.DrawText( clientState.Speed.ToString(), pos + Vector3.left, 15 );
		}
	}
#endif
}
[Serializable]
public class OrientationEvent : UnityEvent<Orientation,Orientation> {}
[Serializable]
public class DualFloatEvent : UnityEvent<float,float> {}
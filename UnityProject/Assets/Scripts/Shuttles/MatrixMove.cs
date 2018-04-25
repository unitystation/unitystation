using System;
using UnityEngine;
using UnityEngine.Networking;

public struct MatrixState
{
	[NonSerialized]
	public bool Inform;
	public bool IsMoving;
	public float Speed;
	public Vector2 Direction; //Direction of movement
	public Vector3 Position;
	/// Matrix rotation. Default is upright (Orientation.Up)
	public Orientation Orientation;

	public static readonly MatrixState Invalid = new MatrixState{Position = CustomNetTransform.InvalidPos};

	public override string ToString() {
		return $"{nameof( Inform )}: {Inform}, {nameof( IsMoving )}: {IsMoving}, {nameof( Speed )}: {Speed}, " +
		       $"{nameof( Direction )}: {Direction}, {nameof( Position )}: {Position}, {nameof( Orientation )}: {Orientation}";
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
	private bool SafetyProtocolsOn { get; set; }
	private bool isMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool isRotatingServer => IsRotatingClient; //todo: calculate rotation time on server instead
	
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
	/// Initial flying direction from editor
	public Vector2 flyingDirection = Vector2.up;
	/// max flying speed from editor
	public float maxSpeed = 20f;
	private readonly float rotSpeed = 6;
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
		Debug.Log( $"Calculated pivot {pivot} for {gameObject.name}" );
		
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
			if ( Input.GetKeyDown( startKey ) ) {
				ToggleMovement();
			}
			if ( Input.GetKeyDown( KeyCode.KeypadPlus ) ) {
				AdjustSpeed( 1 );
			}
			if ( Input.GetKeyDown( KeyCode.KeypadMinus ) ) {
				AdjustSpeed( -1 );
			}
			if ( Input.GetKeyDown( leftKey ) ) {
				TryRotate( false );
			}
			if ( Input.GetKeyDown( rightKey ) ) {
				TryRotate( true );
			}
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
		Debug.Log($"Started moving with speed {serverTargetState.Speed}");
		serverTargetState.IsMoving = true;
		RequestNotify();
	}
	/// Stop movement
	[Server]
	public void StopMovement() {
		Debug.Log("Stopped movement");
		serverTargetState.IsMoving = false;
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
			bool needsRotation = !Mathf.Approximately( transform.rotation.eulerAngles.z, clientState.Orientation.Degree );
			if ( needsRotation ) {
				transform.rotation =
					Quaternion.RotateTowards( transform.rotation, Quaternion.Euler( 0, 0, clientState.Orientation.Degree ),
						Time.deltaTime * 90 );
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
				if ( ServerPositionsMatch ) {
					serverTargetState.Position = goal;
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
			onRotation?.Invoke(clientState.Orientation, newState.Orientation);
		}
		clientState = newState;
		clientTargetState = newState;
	}

	public delegate void OnRotation(Orientation from, Orientation to);
	public event OnRotation onRotation; //fixme: doesn't work for clients

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
		if ( !isMovingServer || serverState.Inform ) {
			MatrixMoveMessage.SendToAll(gameObject, serverState);
			//Clear inform flags
			serverTargetState.Inform = false;
			serverState.Inform = false;
		}
	}

	///     Sync with new player joining
	/// <param name="playerGameObject">player to send to</param>
	[Server]
	public void NotifyPlayer( GameObject playerGameObject )
	{
		MatrixMoveMessage.Send(playerGameObject, gameObject, serverState);
	}

	///Only change orientation if rotation is finished
	[Server]
	public void TryRotate( bool clockwise ) {
		if ( !isRotatingServer ) {
			Rotate(clockwise);
		}
	}
	/// Imperative rotate
	[Server]
	public void Rotate( bool clockwise )
	{
		serverTargetState.Orientation = clockwise ? serverTargetState.Orientation.Next() 
											: serverTargetState.Orientation.Previous();
		//Correcting direction
		Vector3 newDirection = Quaternion.Euler( 0, 0, clockwise ? -90 : 90 ) * serverTargetState.Direction;
//		Debug.Log($"Orientation is now {serverTargetState.Orientation}, Corrected direction from {serverTargetState.Direction} to {newDirection}");
		serverTargetState.Direction = newDirection;
		RequestNotify();
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

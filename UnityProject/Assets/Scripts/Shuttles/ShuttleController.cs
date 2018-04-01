using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public struct MatrixOrientation
{
	public static readonly MatrixOrientation 
		Up = new MatrixOrientation(0),
		Right = new MatrixOrientation(90),
	 	Down = new MatrixOrientation(180),
		Left = new MatrixOrientation(270);
	private static readonly List<MatrixOrientation> sequence = new List<MatrixOrientation> {Up, Left, Down, Right};
	public readonly int degree;

	private MatrixOrientation(int degree)
	{
		this.degree = degree;
	}

	public MatrixOrientation Next()
	{
		int index = sequence.IndexOf(this);
		if (index + 1 >= sequence.Count || index == -1)
		{
			return sequence[0];
		}
		return sequence[index + 1];
	}

	public MatrixOrientation Previous()
	{
		int index = sequence.IndexOf(this);
		if (index <= 0)
		{
			return sequence[sequence.Count-1];
		}
		return sequence[index - 1];
	}

	public override string ToString()
	{
		return $"{degree}";
	}
}

public struct MatrixState
{
	[NonSerialized]
	public bool Inform;
	public bool IsMoving;
	public float Speed;
	public Vector2 Direction; //Direction of movement
	public Vector3 Position;
	/// Matrix rotation. Default is upright (MatrixOrientation.Up)
	public MatrixOrientation Orientation;

	public override string ToString() {
		return $"{nameof( Inform )}: {Inform}, {nameof( IsMoving )}: {IsMoving}, {nameof( Speed )}: {Speed}, " +
		       $"{nameof( Direction )}: {Direction}, {nameof( Position )}: {Position}, {nameof( Orientation )}: {Orientation}";
	}
}

public class ShuttleController : ManagedNetworkBehaviour {
	//server-only values
	public MatrixState State => serverState;
	///used for syncing with players, matters only for server
	private MatrixState serverState;
	/// future state that collects all changes
	private MatrixState serverTargetState; 
	private bool SafetyProtocolsOn { get; set; }
	private bool isMovingServer => serverState.IsMoving && serverState.Speed > 0f;
	private bool ServerPositionsMatch => serverTargetState.Position == serverState.Position;
	private bool isRotatingServer => isRotatingClient; //fixme
	
	//client-only values
	public MatrixState ClientState => clientState;
	///client's transform, can get dirty/predictive
	private MatrixState clientState; 
	/// Is only present to match server's flight routines 
	private MatrixState clientTargetState; 
	private bool isMovingClient => clientState.IsMoving && clientState.Speed > 0f;
	private bool isRotatingClient => transform.rotation.eulerAngles.z != clientState.Orientation.degree;
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

	public override void OnStartServer()
	{
		InitServerState();
		base.OnStartServer();
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
		serverState.Speed = 1f;
		serverState.Position =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		serverState.Orientation = MatrixOrientation.Up;
		serverTargetState = serverState;
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
	private void ToggleMovement() {
		if ( isMovingServer ) {
			StopMovement();
		} else {
			StartMovement();
		}
	}

	[Server]
	private void StartMovement() {
		//Setting speed if there is none
		if ( serverTargetState.Speed <= 0 ) {
			SetSpeed( 1/*, false*/ );
		}
		Debug.Log($"Started moving with speed {serverTargetState.Speed}");
		serverTargetState.IsMoving = true;
		RequestNotify();
	}
	[Server]
	private void StopMovement() {
		Debug.Log("Stopped movement");
		serverTargetState.IsMoving = false;
		RequestNotify();
	}

	[Server]
	private void AdjustSpeed( int relativeValue/*, bool notify = true*/ ) {
		float absSpeed = serverTargetState.Speed += relativeValue;
		SetSpeed( absSpeed/*, notify*/ );
	}

	[Server]
	private void SetSpeed( float absoluteValue/*, bool notify = true*/ ) {
		if ( serverTargetState.Speed <= 0 ) {
			//Stop movement if speed is zero or below
			serverTargetState.Speed = 0;
			if ( serverTargetState.IsMoving ) {
				StopMovement();
			}
			return;
		}
		if ( absoluteValue > maxSpeed ) {
			Debug.LogWarning($"MaxSpeed {maxSpeed} reached, not going further");
			serverTargetState.Speed = maxSpeed;
		} else {
			serverTargetState.Speed = absoluteValue;
		}
		//do not send speed updates when not moving
		if ( isMovingServer ) { 
			RequestNotify();
		}
	}

	private void CheckMovement()
	{
		if ( isRotatingClient ) {
			bool needsRotation = !Mathf.Approximately( transform.rotation.eulerAngles.z, clientState.Orientation.degree );
			if ( needsRotation ) {
				transform.rotation =
					Quaternion.RotateTowards( transform.rotation, Quaternion.Euler( 0, 0, clientState.Orientation.degree ),
						Time.deltaTime * 90 );
			} else {
				// Finishes the job of Lerp and straightens the ship with exact angle value
				transform.rotation = Quaternion.Euler( 0, 0, clientState.Orientation.degree );
			}
		} else if ( isMovingClient ) {
			//Only move target if rotation is finished
			SimulateStateMovement();
		}
		
		//Lerp
		if ( clientState.Position != transform.position ) {
			//todo: is that extra lerp really needed?
			float distance = Vector3.Distance( clientState.Position, transform.position );
			//Activate warp speed if object gets too far away or have to rotate
			bool shouldWarp = distance > 2 || isRotatingClient;
			transform.position =
				Vector3.MoveTowards( transform.position, clientState.Position, clientState.Speed * Time.deltaTime * ( shouldWarp ? 30 : 1 ) );		
		}
	}

	[Server]
	private void CheckMovementServer()
	{
		//ServerState lerping to its target tile
		if ( !ServerPositionsMatch ) {
			serverState.Position =
				Vector3.MoveTowards( serverState.Position,
					serverTargetState.Position,
					serverState.Speed * Time.deltaTime );
			TryNotifyPlayers();
		}
		if ( isMovingServer && !isRotatingServer ) {
			Vector3Int goal = Vector3Int.RoundToInt( serverState.Position + ( Vector3 ) serverTargetState.Direction );
			//    todo: Try to avoid collisions (automatically stop) when safety protocols are on
			if ( !SafetyProtocolsOn || CanMoveTo( goal ) ) {
				//keep moving
				if ( ServerPositionsMatch ) {
					serverTargetState.Position = goal;
				}
			} else {
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
		clientState = newState;
		clientTargetState = newState;
	}

	///predictive perpetual flying
	private void SimulateStateMovement()
	{
//		clientState.Position +=
//			(Vector3) clientState.Direction * clientState.Speed * Time.deltaTime;
		//<<too easy, ends up being faster than server
		//ClientState lerping to its target tile
		if ( !ClientPositionsMatch ) {
			clientState.Position =
				Vector3.MoveTowards( clientState.Position,
					clientTargetState.Position,
					clientState.Speed * Time.deltaTime );
			TryNotifyPlayers();
		}
		if ( isMovingClient && !isRotatingClient ) {
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
			//ClearStateFlags
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

	///Only fly or change orientation if rotation is finished
	[Server]
	private void TryRotate( bool clockwise ) {
		if ( !isRotatingServer ) {
			Rotate(clockwise);
		}
	}

	[Server]
	private void Rotate( bool clockwise )
	{
		serverTargetState.Orientation = clockwise ? serverTargetState.Orientation.Next() 
											: serverTargetState.Orientation.Previous();
		//Correcting direction
		Vector3 newDirection = Quaternion.Euler( 0, 0, clockwise ? -90 : 90 ) * serverTargetState.Direction;
		Debug.Log($"Orientation is now {serverTargetState.Orientation}, Corrected direction from {serverTargetState.Direction} to {newDirection}");
		serverTargetState.Direction = newDirection;
		RequestNotify();
	}
	
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
}

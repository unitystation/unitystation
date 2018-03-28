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
	public bool IsMoving;
	public float Speed;
	public Vector2 Direction; //Direction of movement
	public Vector3 Position;
	/// Matrix rotation. Default is upright (MatrixOrientation.Up)
	public MatrixOrientation Orientation;

	public override string ToString()
	{
		return $"{nameof( IsMoving )}: {IsMoving}, {nameof( Speed )}: {Speed}, {nameof( Direction )}: {Direction}, " +
		       $"{nameof( Position )}: {Position}, {nameof( Orientation )}: {Orientation}";
	}
}

public class ShuttleController : ManagedNetworkBehaviour {
	//server-only values
	public MatrixState State => serverState;
	///used for syncing with players, matters only for server
	private MatrixState serverState; 
	private bool SafetyProtocolsOn { get; set; }
	
	//client-only values
	public MatrixState ClientState => clientState;
	///client's transform, can get dirty/predictive
	private MatrixState clientState; 
	
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
	}

	///managed by UpdateManager
	public override void UpdateMe(){
		if (isServer) {
			if ( Input.GetKeyDown(startKey) ){
				ToggleMovement();
			}
			if ( Input.GetKeyDown(KeyCode.KeypadPlus) )
			{
				AdjustSpeed( 1 );
			}
			if ( Input.GetKeyDown(KeyCode.KeypadMinus) ){
				AdjustSpeed( -1 );
			}
			if ( Input.GetKeyDown(leftKey) )
			{
				TryRotate(false);
			}
			if ( Input.GetKeyDown(rightKey) )
			{
				TryRotate(true);
			}
			CheckMovementServer();
		} 

		CheckMovement();	
	}

	[Server]
	private void ToggleMovement() {
		if ( IsMoving() ) {
			StopMovement();
		} else {
			StartMovement();
		}
	}

	[Server]
	private void StartMovement() {
		//Setting speed if there is none
		if ( serverState.Speed <= 0 ) {
			SetSpeed( 1, false );
		}
		Debug.Log($"Started moving with speed {serverState.Speed}");
		serverState.IsMoving = true;
		NotifyPlayers();
	}
	[Server]
	private void StopMovement() {
		Debug.Log("Stopped movement");
		serverState.IsMoving = false;
		NotifyPlayers();
	}

	[Server]
	private void AdjustSpeed( int relativeValue, bool notify = true ) {
		float absSpeed = serverState.Speed += relativeValue;
		SetSpeed( absSpeed, notify );
	}

	[Server]
	private void SetSpeed( float absoluteValue, bool notify = true ) {
		if ( serverState.Speed <= 0 ) {
			//Stop movement if speed is zero or below
			serverState.Speed = 0;
			if ( serverState.IsMoving ) {
				StopMovement();
			}
			return;
		}
		if ( absoluteValue > maxSpeed ) {
			Debug.LogWarning($"MaxSpeed {maxSpeed} reached, not going further");
			serverState.Speed = maxSpeed;
		} else {
			serverState.Speed = absoluteValue;
		}
		if ( notify ) {
			NotifyPlayers();
		}
	}

	private void CheckMovement()
	{
		if ( NeedsRotation() ) {
			transform.rotation =
				Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, clientState.Orientation.degree),
					Time.deltaTime * 90); 
		} else if ( NeedsFixing() ) {
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = Quaternion.Euler(0, 0, clientState.Orientation.degree);
		}
		//todo: Only fly or change orientation if rotation is finished
		//Move target
		if ( IsMoving() ) {
			SimulateStateMovement();
		}
		//Lerp
		if ( clientState.Position != transform.position ) {
			transform.position =
				Vector3.MoveTowards(transform.position, clientState.Position, clientState.Speed * Time.deltaTime);		
		}
	}

	[Server]
	private void CheckMovementServer()
	{
		if ( IsMoving() ) {
			Vector3 newGoal = serverState.Position +
			                  ( Vector3 ) serverState.Direction * serverState.Speed * Time.deltaTime;
			Vector3Int intGoal = CustomNetTransform.RoundWithContext( newGoal, serverState.Direction );
			//    todo: Try to avoid collisions (automatically stop) when safety protocols are on
			if ( !SafetyProtocolsOn || CanMoveTo( intGoal ) ) {
				serverState.Position = newGoal;
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
	public void SetPosition( Vector3 pos, bool notify = true )
	{
		serverState.Position = pos;
		if (notify) {
			NotifyPlayers();
		}
	}
	
	/// Called when MatrixMoveMessage is received
	public void UpdateClientState( MatrixState newState )
	{
		clientState = newState;
	}

	///  Currently sending to everybody, but should be sent to nearby players only
	[Server]
	private void NotifyPlayers()
	{
		MatrixMoveMessage.SendToAll(gameObject, serverState);
	}
	
	///     Sync with new player joining
	/// <param name="playerGameObject"></param>
	[Server]
	public void NotifyPlayer( GameObject playerGameObject )
	{
		MatrixMoveMessage.Send(playerGameObject, gameObject, serverState);
	}

	private bool IsMoving()
	{
		if (isServer)
		{
			return serverState.IsMoving && serverState.Speed > 0f;
		}
		return clientState.IsMoving && clientState.Speed > 0f;
	}
	
	//todo: IsRotating()
	
	///predictive perpetual flying
	private void SimulateStateMovement()
	{
		clientState.Position +=
			(Vector3) clientState.Direction * clientState.Speed * Time.deltaTime;
	}
	
	private bool NeedsFixing()
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		return transform.rotation.eulerAngles.z != clientState.Orientation.degree;
	}
	private bool NeedsRotation()
	{
		return !Mathf.Approximately(transform.rotation.eulerAngles.z, clientState.Orientation.degree);
	}

	///Only fly or change orientation if rotation is finished
	[Server]
	private void TryRotate( bool clockwise ) {
		//todo: Only fly or change orientation if rotation is finished
		Rotate(clockwise);
	}

	[Server]
	private void Rotate( bool clockwise )
	{
		serverState.Orientation = clockwise ? serverState.Orientation.Next() 
											: serverState.Orientation.Previous();
		//Correcting direction
		Vector3 newDirection = Quaternion.Euler( 0, 0, clockwise ? -90 : 90 ) * serverState.Direction;
		Debug.Log($"Orientation is now {serverState.Orientation}, Corrected direction from {serverState.Direction} to {newDirection}");
		serverState.Direction = newDirection;
		NotifyPlayers();
	}
	
	//Visual debug
	private Vector3 size1 = Vector3.one;
	private Vector3 size2 = new Vector3( 0.9f, 0.9f, 0.9f );
	private Color color1 = Color.red;
	private Color color2 = Color.white;

	private void OnDrawGizmos() {
		//serverState
		Gizmos.color = color1;
		Vector3 serverPos = serverState.Position;
		Gizmos.DrawWireCube( serverPos, size1 );
		if ( serverState.IsMoving ) {
			GizmoUtils.DrawArrow( serverPos + Vector3.right / 5, serverState.Direction * serverState.Speed );
			GizmoUtils.DrawText( serverState.Speed.ToString(), serverPos + Vector3.right, 15 );
		}
		//clientState
		Gizmos.color = color2;
		Vector3 pos = clientState.Position;
		Gizmos.DrawWireCube( pos, size2 );
		if ( clientState.IsMoving ) {
			GizmoUtils.DrawArrow( pos + Vector3.left / 5, clientState.Direction * clientState.Speed );
			GizmoUtils.DrawText( clientState.Speed.ToString(), pos + Vector3.left, 15 );
		}
	}
}

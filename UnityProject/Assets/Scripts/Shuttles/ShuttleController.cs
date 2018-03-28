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
	public MatrixState State => serverTransformState;
	///used for syncing with players, matters only for server
	private MatrixState serverTransformState; 
	private bool SafetyProtocolsOn { get; set; }
	
	//client-only values
	public MatrixState ClientState => transformState;
	///client's transform, can get dirty/predictive
	private MatrixState transformState; 
	
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
			serverTransformState.Direction = Vector2.up;
		} else {
			serverTransformState.Direction = Vector2Int.RoundToInt(flyingDirection);
		}
		serverTransformState.Speed = 1f;
		serverTransformState.Position =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		serverTransformState.Orientation = MatrixOrientation.Up;
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
		//			CheckSafetyProtocols();
		} 

			CheckMovement();	
	}

	[Server]
	private void ToggleMovement() {
		serverTransformState.IsMoving = !serverTransformState.IsMoving;
		if ( serverTransformState.IsMoving ) {
			Debug.Log($"Started moving with speed {serverTransformState.Speed}");
		} else {
			Debug.Log("Stopped movement");
		}
		NotifyPlayers();
	}

	[Server]
	private void AdjustSpeed(int relativeValue) {
		float absSpeed = serverTransformState.Speed += relativeValue;
		TrySetSpeed(absSpeed);
	}

	[Server]
	private void SetSpeed(float absoluteValue) {
		TrySetSpeed(absoluteValue);
	}

	[Server]
	private void TrySetSpeed(float proposedSpeed) {
		if ( serverTransformState.Speed <= 0 ) {
			//Stop movement if speed is zero or below
			serverTransformState.Speed = 1;
			ToggleMovement();
			return;
		}
		if ( proposedSpeed > maxSpeed ) {
			Debug.LogWarning($"MaxSpeed {maxSpeed} reached, not going further");
			serverTransformState.Speed = maxSpeed;
		} else {
			serverTransformState.Speed = proposedSpeed;
		}
		NotifyPlayers();
	}

	private void CheckMovement()
	{
		if ( NeedsRotation() ) {
			transform.rotation =
				Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, transformState.Orientation.degree),
					Time.deltaTime * 90); 
		} else if ( NeedsFixing() ) {
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = Quaternion.Euler(0, 0, transformState.Orientation.degree);
		}
		//todo: Only fly or change orientation if rotation is finished
		if ( IsMoving() ) {
			SimulateStateMovement();
		}
		if ( transformState.Position != transform.position ) {
			transform.Translate(transformState.Direction * transformState.Speed * Time.deltaTime);
		}
	}

//	///     Try to avoid collisions (automatically stop) when safety protocols are on
//	[Server]
//	private void CheckSafetyProtocols()
//	{
//		if (IsMoving() && SafetyProtocolsOn)
//		{
//			Vector3 newGoal = serverTransformState.Position +
//			                  (Vector3) serverTransformState.Direction * serverTransformState.Speed * Time.deltaTime;
//			Vector3Int intGoal = CustomNetTransform.RoundWithContext(newGoal, serverTransformState.Direction);
//			if (CanMoveTo(intGoal))
//			{
//				//Spess drifting
//				serverTransformState.Position = newGoal;
//			}
//			else //Stopping drift
//			{
//				serverTransformState.Direction = Vector2.zero; 
//				NotifyPlayers();
//			}
//		}
//	}
//	
//	private bool CanMoveTo(Vector3Int goal)
//	{
//		//todo: safety protocols
//		return true;
//	}
	
	/// Manually set matrix to a specific position.
	[Server]
	public void SetPosition(Vector3 pos, bool notify = true)
	{
		serverTransformState.Position = pos;
		if (notify) {
			NotifyPlayers();
		}
	}
	
	/// Called when MatrixMoveMessage is received
	public void UpdateClientState(MatrixState newState)
	{
		transformState = newState;
	}

	///  Currently sending to everybody, but should be sent to nearby players only
	[Server]
	private void NotifyPlayers()
	{
		//todo: move message
		MatrixMoveMessage.SendToAll(gameObject, serverTransformState);
	}
	
	///     Sync with new player joining
	/// <param name="playerGameObject"></param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject)
	{
		//todo: move message
		MatrixMoveMessage.Send(playerGameObject, gameObject, serverTransformState);
	}

	private bool IsMoving()
	{
		if (isServer)
		{
			return transformState.IsMoving && serverTransformState.Speed > 0f;
		}
		return transformState.IsMoving && transformState.Speed > 0f;
	}
	
	///predictive perpetual flying
	private void SimulateStateMovement()
	{
		transformState.Position +=
			(Vector3) transformState.Direction * transformState.Speed * Time.deltaTime;
	}
	
	private bool NeedsFixing()
	{
		// ReSharper disable once CompareOfFloatsByEqualityOperator
		return transform.rotation.eulerAngles.z != transformState.Orientation.degree;
	}

	private bool NeedsRotation()
	{
		return !Mathf.Approximately(transform.rotation.eulerAngles.z, transformState.Orientation.degree);
	}

	///Only fly or change orientation if rotation is finished
	[Server]
	private void TryRotate(bool clockwise) {
		//todo: Only fly or change orientation if rotation is finished
		Rotate(clockwise);
	}

	[Server]
	private void Rotate(bool clockwise)
	{
		serverTransformState.Orientation = clockwise ? serverTransformState.Orientation.Next() 
													 : serverTransformState.Orientation.Previous();
		Debug.Log($"Orientation is now {serverTransformState.Orientation}");
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
		Vector3 serverPos = serverTransformState.Position;
		Gizmos.DrawWireCube( serverPos - CustomNetTransform.deOffset, size1 );
		if ( serverTransformState.IsMoving ) {
			GizmoUtils.DrawArrow( serverPos + Vector3.right / 5, serverTransformState.Direction * serverTransformState.Speed );
			GizmoUtils.DrawText( serverTransformState.Speed.ToString(), serverPos + Vector3.right, 15 );
		}
		//clientState
		Gizmos.color = color2;
		Vector3 pos = transformState.Position;
		Gizmos.DrawWireCube( pos, size2 );
		if ( transformState.IsMoving ) {
			GizmoUtils.DrawArrow( pos + Vector3.left / 5, transformState.Direction * transformState.Speed );
			GizmoUtils.DrawText( transformState.Speed.ToString(), pos + Vector3.left, 15 );
		}
	}
}

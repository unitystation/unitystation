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
//	public bool Active;
	public float Speed;
	public Vector2 Impulse; //Direction of movement
	public Vector3 Position;
	public MatrixOrientation Orientation;

	public override string ToString()
	{
		return $"[{nameof( Position )}: {Position}, {nameof( Orientation )}: {Orientation}, {nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}]";
	}
}

public class ShuttleController : ManagedNetworkBehaviour {
	public MatrixState State => serverTransformState;
	public MatrixState ClientState => transformState;
	///used for syncing with players, matters only for server
	private MatrixState serverTransformState; 
	///client's transform, can get dirty/predictive
	private MatrixState transformState; 

	
	//TEST MODE
	bool doFlyingThing;
	public Vector2 flyingDirection;
	public float speed;
	private readonly float rotSpeed = 6;
	public KeyCode startKey = KeyCode.G;
	public KeyCode leftKey = KeyCode.Keypad4;
	public KeyCode rightKey = KeyCode.Keypad6;
	private bool SafetyProtocolsOn { get; set; }



	//	private MatrixOrientation orientation = MatrixOrientation.Up;


	public override void OnStartServer()
	{
		InitServerState();
		base.OnStartServer();
	}

	[Server]
	private void InitServerState()
	{
		serverTransformState.Position =
			Vector3Int.RoundToInt(new Vector3(transform.position.x, transform.position.y, 0));
		serverTransformState.Orientation = MatrixOrientation.Up;
	}

	//managed by UpdateManager

	public override void UpdateMe(){
		if ( Input.GetKeyDown(startKey) ){
			doFlyingThing = !doFlyingThing;
		}
		if ( Input.GetKeyDown(KeyCode.KeypadPlus) ){
			speed++;
		}
		if ( Input.GetKeyDown(KeyCode.KeypadMinus) ){
			speed--;
		}
		
		if (isServer)
		{
			CheckSafetyProtocols();
		} 
		
		if (IsMoving())
		{
			SimulateMovement();
			//fixme: don't simulate moving through solid stuff on client
		}

		if (transformState.Position != transform.position)
		{
			Lerp();
		}
		
		CheckMovement();	
	}

	private void CheckMovement()
	{
		if ( NeedsRotation() )
		{
			transform.rotation =
				Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0, 0, transformState.Orientation.degree),
					Time.deltaTime * 90); //transform.Rotate(Vector3.forward * rotSpeed * Time.deltaTime);
		}
		else if ( NeedsFixing() )
		{
			// Finishes the job of Lerp and straightens the ship with exact angle value
			transform.rotation = Quaternion.Euler(0, 0, transformState.Orientation.degree);
		}
		else
		{
			//Only fly or change orientation if rotation is finished
			if ( Input.GetKeyDown(leftKey) )
			{
				Rotate(false);
			}
			if ( Input.GetKeyDown(rightKey) )
			{
				Rotate(true);
			}
			if ( IsMoving() )
			{
				transform.Translate(flyingDirection * speed * Time.deltaTime);
			}
		}
	}

	///     Try to avoid collisions (automatically stop) when safety protocols are on
	[Server]
	private void CheckSafetyProtocols()
	{
		if (IsMoving() && SafetyProtocolsOn)
		{
			Vector3 newGoal = serverTransformState.Position +
			                  (Vector3) serverTransformState.Impulse * serverTransformState.Speed * Time.deltaTime;
			Vector3Int intGoal = CustomNetTransform.RoundWithContext(newGoal, serverTransformState.Impulse);
			if (CanMoveTo(intGoal))
			{
				//Spess drifting
				serverTransformState.Position = newGoal;
			}
			else //Stopping drift
			{
				serverTransformState.Impulse = Vector2.zero; 
				NotifyPlayers();
			}
		}
	}

	/// Manually set matrix to a specific position.
	[Server]
	public void SetPosition(Vector3 pos, bool notify = true, float speed = 4f)
	{
		UpdateServerTransformState(pos, notify, speed);
	}
	
	[Server]
	private void UpdateServerTransformState(Vector3 pos, bool notify = true, float speed = 4f){
		serverTransformState.Speed = speed;
		serverTransformState.Position = pos;
		if (notify) {
			NotifyPlayers();
		}
	}
	
	/// <summary>
	///     Currently sending to everybody, but should be sent to nearby players only
	/// </summary>
	[Server]
	private void NotifyPlayers()
	{
		//todo: move message
//		MatrixMoveMessage.SendToAll(gameObject, serverTransformState);
	}
	
	/// <summary>
	///     Sync with new player joining
	/// </summary>
	/// <param name="playerGameObject"></param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject)
	{
		//todo: move message
//		MatrixMoveMessage.Send(playerGameObject, gameObject, serverTransformState);
	}

	private bool IsMoving()
	{
		if (isServer)
		{
			return serverTransformState.Impulse != Vector2.zero && serverTransformState.Speed != 0f;
		}
		return transformState.Impulse != Vector2.zero && transformState.Speed != 0f;
	}
	
	private bool CanMoveTo(Vector3Int goal)
	{
		//todo: safety protocols
		return true;
	}
	
	///predictive perpetual flying
	private void SimulateMovement()
	{
		transformState.Position +=
			(Vector3) transformState.Impulse * transformState.Speed * Time.deltaTime;
	}
	private void Lerp()
	{
		if ( transformState.Speed.Equals(0) )
		{
			transform.localPosition = transformState.Position;
			return;
		}
		transform.localPosition =
			Vector3.MoveTowards(transform.localPosition, transformState.Position, transformState.Speed * Time.deltaTime);
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

	[Server]
	private void Rotate(bool clockwise)
	{
		serverTransformState.Orientation = clockwise ? serverTransformState.Orientation.Next() 
													 : serverTransformState.Orientation.Previous();
		Debug.Log($"Orientation is now {serverTransformState.Orientation}");
		NotifyPlayers();
	}
}

using System;
using Tilemaps;
using Tilemaps.Behaviours.Layers;
using Tilemaps.Behaviours.Objects;
using UnityEngine;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

// ReSharper disable CompareOfFloatsByEqualityOperator
public struct TransformState
{
	public bool Active;
	public float Speed;
	///Direction of throw
	public Vector2 Impulse;

	public int MatrixId;
	public Vector3 Position;

	/// Means that this object is hidden
	public static readonly Vector3Int HiddenPos = new Vector3Int(0, 0, -100);
	/// Means that this object is hidden
	public static readonly TransformState HiddenState = new TransformState{Active = false, Position = HiddenPos, MatrixId = 0};

	public Vector3 WorldPosition
	{
		get
		{
			if (Position == HiddenPos || Active == false)
			{
				return Position;
			}
			MatrixManager matrixManager = MatrixManager.Instance;
			if ( !matrixManager ) {
				Debug.LogWarning( "MatrixManager not initialized" );
				return Position;
			}
			MatrixInfo matrix = matrixManager.Get( MatrixId );
			return MatrixManager.LocalToWorld( Position, matrix );
		}
		set
		{
			if (value == HiddenPos) {
				this = HiddenState;
			}
			else
			{
				MatrixManager matrixManager = MatrixManager.Instance;
				if ( !matrixManager ) {
					Debug.LogWarning( "MatrixManager not initialized" );
					return;
				}
				MatrixInfo matrix = matrixManager.Get( MatrixId );
				Position = MatrixManager.WorldToLocal( value, matrix );
			}
		}
	}

	public override string ToString()
	{
		return Equals( HiddenState ) ? "[Hidden]" : $"[{nameof( Active )}: {Active}, {nameof( Position )}: {(Vector2)Position}, {nameof( WorldPosition )}: {(Vector2)WorldPosition}, " +
		       $"{nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}, {nameof( MatrixId )}: {MatrixId}]";
	}
}

public class CustomNetTransform : ManagedNetworkBehaviour //see UpdateManager
{
	private RegisterTile registerTile;

	private TransformState serverState = TransformState.HiddenState; //used for syncing with players, matters only for server

	public float SpeedMultiplier = 1; //Multiplier for flying/lerping speed, could corelate with weight, for example
	private TransformState clientState = TransformState.HiddenState; //client's transform, can get dirty/predictive

	private Matrix matrix => registerTile.Matrix;
	
	public TransformState ServerState => serverState;
	public TransformState ClientState => clientState;

//	[SyncVar]
	public bool isPushing;
	public bool predictivePushing = false;

	private void Start()
	{
		clientState = TransformState.HiddenState;
		registerTile = GetComponent<RegisterTile>();
	}

	public override void OnStartServer()
	{
		InitServerState();
		base.OnStartServer();
	}

	[Server]
	private void InitServerState()
	{
//		isPushing = false;
//		predictivePushing = false;

		serverState.Speed = 0;
		if ( !Vector3Int.RoundToInt( (Vector2)transform.position ).Equals( TransformState.HiddenPos )
		  && !Vector3Int.RoundToInt( (Vector2)transform.localPosition ).Equals( TransformState.HiddenPos ) )
		{
			serverState.Active = true;
			serverState.Position =
				Vector3Int.RoundToInt(new Vector3(transform.localPosition.x, transform.localPosition.y, 0));
		}
//		registerTile = GetComponent<RegisterTile>();
//		if ( registerTile && registerTile.Matrix && MatrixManager.Instance ) {
//			serverState.MatrixId = MatrixManager.Instance.Get( matrix ).Id;
//		} else {
//			Debug.LogWarning( "Matrix id init failure" );
//		}
	}

	/// Intended for runtime spawning, so that CNT could initialize accordingly
	[Server]
	public void ReInitServerState()
	{
		InitServerState();
	//	Debug.Log($"{name} reInit: {serverTransformState}");
	}

//	/// Overwrite server state with a completely new one
//    [Server]
//    public void SetState(TransformState state)
//    {
//        serverTransformState = state;
//        NotifyPlayers();
//    }

	/// Manually set an item to a specific position. Use WorldPosition!
	[Server]
	public void SetPosition(Vector3 worldPos, bool notify = true, float speed = 4f, bool _isPushing = false) {
//		Only allow one movement at a time if it is currently being pushed
//		if(isPushing || predictivePushing){
//			if(predictivePushing && _isPushing){
//				This is if the server player is pushing because the predictive flag
				//will be set early we still need to notify the players so call it here:
//				UpdateServerTransformState(pos, notify, speed);
				//And then set the isPushing flag:
//				isPushing = true;
//			}
//			return;
//		}
		var pos = (Vector2) worldPos; //Cut z-axis
		serverState.MatrixId = MatrixManager.Instance.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
		serverState.Speed = speed;
		serverState.WorldPosition = pos;
		if (notify) {
			NotifyPlayers();
		}

		//Set it to being pushed if it is a push net action
//		if(_isPushing){
			//This is synced via syncvar with all players
//			isPushing = true;
//		}
	}

	/// Apply impulse while setting position
	[Server]
	public void PushTo(Vector3 pos, Vector2 impulseDir, bool notify = true, float speed = 4f, bool _isPushing = false)
	{
//		if (IsInSpace()) {
//			serverTransformState.Impulse = impulseDir;
//		} else {
//			SetPosition(pos, notify, speed, _isPushing);
//		}
	}

	[Server]
	private void SyncMatrix() {
		registerTile.ParentNetId = MatrixManager.Instance.Get( serverState.MatrixId ).NetId;
	}

	/// <summary>
	///     Dropping with some force, in random direction. For space floating demo purposes.
	/// </summary>
	[Server]
	public void ForceDrop(Vector3 pos)
	{
//		GetComponentInChildren<SpriteRenderer>().color = Color.white;
		serverState.Active = true;
		serverState.WorldPosition = pos;
		Vector2 impulse = Random.insideUnitCircle.normalized;
		//don't apply impulses if item isn't going to float in that direction
		Vector3Int newGoal = RoundWithContext(serverState.WorldPosition + (Vector3) impulse, impulse);
		if (CanDriftTo(newGoal))
		{
//			Debug.LogFormat($"ForceDrop success: from {pos} to {localToWorld(newGoal)}");
			serverState.Impulse = impulse;
			serverState.Speed = Random.Range(0.5f, 3f);
		}
//		else
//		{
//			Debug.LogWarningFormat($"ForceDrop fail: from {pos} to {localToWorld(newGoal)}");			
//		}
		NotifyPlayers();
	}

	[Server]
	public void DisappearFromWorldServer()
	{
		serverState = TransformState.HiddenState;
		NotifyPlayers();
	}

	[Server]
	public void AppearAtPositionServer(Vector3 worldPos)
	{
		serverState.Active = true;
		SetPosition(worldPos);
	}

	/// <summary>
	///     Method to substitute transform.parent = x stuff.
	///     You shouldn't really use it anymore,
	///     as there are high level methods that should suit your needs better.
	///     Server-only, client is not being notified
	/// </summary>
	[Server]
	public void SetParent(Transform pos)
	{
		transform.parent = pos;
	}

	/// <summary>
	///     Convenience method to make stuff disappear at position.
	///     For CLIENT prediction purposes.
	/// </summary>
	public void DisappearFromWorld()
	{
		clientState.Active = false;
		clientState.Position = TransformState.HiddenPos;
//		clientState = TransformState.HiddenState;
		updateActiveStatus();
	}

	/// <summary>
	///     Convenience method to make stuff appear at position
	///     For CLIENT prediction purposes.
	/// </summary>
	public void AppearAtPosition(Vector3 pos)
	{
		clientState.Active = true;
		clientState.WorldPosition = pos;
		transform.position = pos;
		updateActiveStatus();
	}

	/// <summary>
	/// Client side prediction for pushing
	/// This allows instant pushing reaction to a pushing event
	/// on the client who instigated it. The server then validates
	/// the transform position and returns it if it is illegal
	/// </summary>
	public void PushToPosition(Vector3 pos, float speed, PushPull pushComponent)
	{
//		if(pushComponent.pushing || predictivePushing){
//			return;
//		}
//		TransformState newState = clientState;
//		newState.Active = true;
//		newState.Speed = speed;
//		newState.Position = pos;
//		UpdateClientState(newState);
//		predictivePushing = true;
//		pushComponent.pushing = true;
	}

	public void UpdateClientState(TransformState newState)
	{
		//Don't lerp (instantly change pos) if active state was changed
		if (clientState.Active != newState.Active /*|| newState.Speed == 0*/)
		{
			transform.localPosition = newState.Position;
		}
		clientState = newState;
		updateActiveStatus();
	}

	private void updateActiveStatus()
	{
		if (clientState.Active)
		{
			RegisterObjects();
		}
		else
		{
			registerTile.Unregister();
		}
		//todo: Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example. 
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = clientState.Active;
		}
	}

	/// <summary>
	///     Currently sending to everybody, but should be sent to nearby players only
	/// </summary>
	[Server]
	private void NotifyPlayers()
	{
		SyncMatrix();
		TransformStateMessage.SendToAll(gameObject, serverState);
	}

	/// <summary>
	///     Sync with new player joining
	/// </summary>
	/// <param name="playerGameObject"></param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject)
	{
		TransformStateMessage.Send(playerGameObject, gameObject, serverState);
	}


	//managed by UpdateManager
	public override void UpdateMe()
	{
		if (!registerTile)
		{
			registerTile = GetComponent<RegisterTile>();
		}
		Synchronize();
	}

	private void RegisterObjects()
	{
		//Register item pos in matrix
		registerTile.UpdatePosition();
	}

	private void Synchronize()
	{
		if (!clientState.Active)
		{
			return;
		}

		if (isServer)
		{
			CheckSpaceDrift();
//			//Sync the pushing state to all players
//			//this makes sure that players with high pings cannot get too
//			//far with prediction
//			if(isPushing){
//				if(clientState.Position == transform.localPosition){
//					isPushing = false;
//					predictivePushing = false;
//				}
//			}
		} 

		if (IsFloating())
		{
			SimulateFloating();
			//fixme: don't simulate moving through solid stuff on client
		}

		if (clientState.Position != transform.localPosition)
		{
			Lerp();
		}

		//Registering
		if (registerTilePos() != Vector3Int.RoundToInt(clientState.Position) )//&& !isPushing && !predictivePushing)
		{
//			Debug.LogFormat($"registerTile updating {localToWorld(registerTilePos())}->{localToWorld(Vector3Int.RoundToInt(transform.localPosition))}, " +
//			                $"ts={localToWorld(Vector3Int.RoundToInt(transformState.localPos))}");
			RegisterObjects();
		}
	}

	///predictive perpetual flying
	private void SimulateFloating()
	{
		clientState.Position +=
			(Vector3) clientState.Impulse * (clientState.Speed * SpeedMultiplier) * Time.deltaTime;
	}

	private Vector3Int registerTilePos()
	{
		return registerTile.Position;
	}

	private void Lerp()
	{
		if ( clientState.Speed.Equals(0) )
		{
			transform.localPosition = clientState.Position;
			return;
		}
		transform.localPosition =
			Vector3.MoveTowards(transform.localPosition, clientState.Position, clientState.Speed * SpeedMultiplier * Time.deltaTime);
	}

	/// <summary>
	///     Space drift detection is serverside only
	/// </summary>
	[Server]
	private void CheckSpaceDrift()
	{
		if (IsFloating() && matrix != null)
		{
			Vector3 newGoal = serverState.Position +
			                                      (Vector3) serverState.Impulse * (serverState.Speed * SpeedMultiplier) * Time.deltaTime;
			Vector3Int intGoal = RoundWithContext(newGoal, serverState.Impulse);
			if (CanDriftTo(intGoal))
			{
				if (registerTile.Position != Vector3Int.RoundToInt(transform.localPosition)){
					registerTile.UpdatePosition();
//					RpcForceRegisterUpdate();
				}
				//Spess drifting
				serverState.Position = newGoal;
			}
			else //Stopping drift
			{
				serverState.Impulse = Vector2.zero; //killing impulse, be aware when implementing throw!
				NotifyPlayers();
				registerTile.UpdatePosition();
//				RpcForceRegisterUpdate();
			}
		}
	}

//	//For space drift, the server will confirm an update is required and inform the clients
//	[ClientRpc]
//	private void RpcForceRegisterUpdate(){
//		registerTile.UpdatePosition();
//	}

	///Special rounding for collision detection
	///returns V3Int of next tile
	public static Vector3Int RoundWithContext(Vector3 roundable, Vector2 impulseContext)
	{
		float x = impulseContext.x;
		float y = impulseContext.y;
		return new Vector3Int(
			x < 0 ? (int) Math.Floor(roundable.x) : (int) Math.Ceiling(roundable.x),
			y < 0 ? (int) Math.Floor(roundable.y) : (int) Math.Ceiling(roundable.y),
			0);
	}

	public bool IsInSpace(){
		return MatrixManager.Instance.IsSpaceAt( Vector3Int.RoundToInt( transform.position ) );
	}

	public bool IsFloating()
	{
		if (isServer)
		{
			return serverState.Impulse != Vector2.zero && serverState.Speed != 0f;
		}
		return clientState.Impulse != Vector2.zero && clientState.Speed != 0f;
	}

	/// Make sure to use localPos when asking
	private bool CanDriftTo(Vector3Int goal)
	{
		return matrix != null && matrix.IsEmptyAt(goal);
	}
}
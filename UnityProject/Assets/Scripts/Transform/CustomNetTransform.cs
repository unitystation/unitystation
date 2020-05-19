using System.Collections;
using DatabaseAPI;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

// ReSharper disable CompareOfFloatsByEqualityOperator

public partial class CustomNetTransform : ManagedNetworkBehaviour, IPushable //see UpdateManager
{
	//I think this is valid server side only
	public bool VisibleState {
		get => ServerPosition != TransformState.HiddenPos;
		set => SetVisibleServer( value );
	}

	private Vector3IntEvent onUpdateReceived = new Vector3IntEvent();
	public Vector3IntEvent OnUpdateRecieved() {
		return onUpdateReceived;
	}
	/// Is also invoked in perpetual space flights
	private DualVector3IntEvent onStartMove = new DualVector3IntEvent();
	public DualVector3IntEvent OnStartMove() => onStartMove;
	private DualVector3IntEvent onClientStartMove = new DualVector3IntEvent();
	public DualVector3IntEvent OnClientStartMove() => onClientStartMove;
	private Vector3IntEvent onTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnTileReached() => onTileReached;
	private Vector3IntEvent onClientTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnClientTileReached() => onClientTileReached;

	public CollisionEvent onHighSpeedCollision = new CollisionEvent();
	public CollisionEvent OnHighSpeedCollision() => onHighSpeedCollision;

	private UnityEvent onPullInterrupt = new UnityEvent();
	public UnityEvent OnPullInterrupt() => onPullInterrupt;

	public ThrowEvent OnThrowStart = new ThrowEvent();
	public ThrowEvent OnThrowEnd = new ThrowEvent();

	public bool IsFixedMatrix = false;

	private OccupiableDirectionalSprite occupiableDirectionalSprite = null;

	/// <summary>
	/// If it has ItemAttributes, get size from it (default to tiny).
	/// Otherwise it's probably something like a locker, so consider it huge.
	/// </summary>
	public ItemSize Size
	{
		get
		{
			if ( ItemAttributes == null )
			{
				return ItemSize.Huge;
			}
			if ( ItemAttributes.Size == 0 )
			{
				return ItemSize.Tiny;
			}
			return ItemAttributes.Size;
		}
	}

	public Vector3Int ServerPosition => serverState.WorldPosition.RoundToInt();
	public Vector3Int ServerLocalPosition => serverState.Position.RoundToInt();
	public Vector3Int ClientPosition => predictedState.WorldPosition.RoundToInt();
	public Vector3Int ClientLocalPosition => predictedState.Position.RoundToInt();
	public Vector3Int TrustedPosition => clientState.WorldPosition.RoundToInt();
	public Vector3Int TrustedLocalPosition => clientState.Position.RoundToInt();
	public Vector3Int LastNonHiddenPosition { get; } = TransformState.HiddenPos; //todo: implement for CNT!

	/// <summary>
	/// Used to determine if this transform is worth updating every frame
	/// </summary>
	private enum MotionStateEnum { Moving, Still }

	private MotionStateEnum motionState = MotionStateEnum.Moving;
	/// <summary>
	/// Used to determine if this transform is worth updating every frame
	/// </summary>
	private MotionStateEnum MotionState
	{
		get { return motionState; }
		set
		{
			if ( motionState == value || !UpdateManager.IsInitialized )
			{
				return;
			}

			if ( value == MotionStateEnum.Moving )
			{
				doMotionCheck = false;
				// In the case we become Still and then Moving again in one second, we're still updating because freeze timer hasn't finished yet.
				// Checking here if timer has passed yet (so we're no longer updating), if we are still updating we don't have to call OnEnable again.
				if (!IsUpdating)
					base.OnEnable();
			}
			else
			{
				motionCheckTime = 0f;
				doMotionCheck = true;
			}

			motionState = value;
		}
	}

	private bool doMotionCheck = false;
	private float motionCheckTime = 0f;

	private RegisterTile registerTile;
	public RegisterTile RegisterTile => registerTile;

	private ItemAttributesV2 ItemAttributes {
		get {
			if ( itemAttributes == null ) {
				itemAttributes = GetComponent<ItemAttributesV2>();
			}
			return itemAttributes;
		}
	}
	private ItemAttributesV2 itemAttributes;

	private TransformState serverState = TransformState.Uninitialized; //used for syncing with players, matters only for server
	private TransformState serverLerpState = TransformState.Uninitialized; //used for simulating lerp on server

	private TransformState clientState = TransformState.Uninitialized; //last reliable state from server
	private TransformState predictedState = TransformState.Uninitialized; //client's transform, can get dirty/predictive

	private Matrix matrix => registerTile.Matrix;

	public TransformState ServerState => serverState;
	public TransformState ServerLerpState => serverLerpState;
	public TransformState PredictedState => predictedState;
	public TransformState ClientState => clientState;

	private void Start()
	{
		registerTile = GetComponent<RegisterTile>();
		itemAttributes = GetComponent<ItemAttributesV2>();
		var _pushPull = PushPull; //init
		OnUpdateRecieved().AddListener( Poke );
		occupiableDirectionalSprite = GetComponent<OccupiableDirectionalSprite>();
	}
	/// <summary>
	/// Subscribes this CNT to Update() cycle
	/// </summary>
	private void Poke()
	{
		Poke(TransformState.HiddenPos);
	}
	/// <summary>
	/// Subscribes this CNT to Update() cycle
	/// </summary>
	/// <param name="v">unused and ignored</param>
	private void Poke( Vector3Int v )
	{
		MotionState = MotionStateEnum.Moving;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		Collider2D[] colls = GetComponents<Collider2D>();
		foreach (var c in colls)
		{
			c.enabled = false;
		}

		if ( !registerTile )
		{
			registerTile = GetComponent<RegisterTile>();
		}

		registerTile.WaitForMatrixInit(InitClientState);
	}

	private void InitClientState(MatrixInfo info)
	{
		CustomNetTransformNewPlayer.Send(netId);
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InitServerState();
	}

	[Server]
	private void InitServerState()
	{
		if ( IsHiddenOnInit ) {
			return;
		}

		if ( !registerTile )
		{
			registerTile = GetComponent<RegisterTile>();
		}

		registerTile.WaitForMatrixInit(PerformServerStateInit);
	}

	private void PerformServerStateInit(MatrixInfo info)
	{
		//If object is supposed to be hidden, keep it that way
		serverState.Speed = 0;
		serverState.SpinRotation = transform.localRotation.eulerAngles.z;
		serverState.SpinFactor = 0;

		if ( registerTile )
		{
			MatrixInfo matrixInfo = MatrixManager.Get( transform.parent );

			if ( matrixInfo == MatrixInfo.Invalid )
			{
				Logger.LogWarning( $"{gameObject.name}: was unable to detect Matrix by parent!", Category.Transform );
				serverState.MatrixId = MatrixManager.AtPoint( ( (Vector2)transform.position ).RoundToInt(), true ).Id;
			} else
			{
				serverState.MatrixId = matrixInfo.Id;
			}
			serverState.Position = ((Vector2)transform.localPosition).RoundToInt();

		} else
		{
			serverState.MatrixId = 0;
			Logger.LogWarning( $"{gameObject.name}: unable to detect MatrixId!", Category.Transform );
		}

		registerTile.UpdatePositionServer();

		serverLerpState = serverState;
		NotifyPlayers();
	}

	/// Is it supposed to be hidden? (For init purposes)
	private bool IsHiddenOnInit =>
		Vector3Int.RoundToInt( transform.position ).Equals( TransformState.HiddenPos ) ||
		Vector3Int.RoundToInt( transform.localPosition ).Equals( TransformState.HiddenPos );

	/// Intended for runtime spawning, so that CNT could initialize accordingly
	[Server]
	public void ReInitServerState()
	{
		InitServerState();
	//	Logger.Log($"{name} reInit: {serverTransformState}");
	}

	public void RollbackPrediction() {
		predictedState = clientState;
	}

	//managed by UpdateManager
	public override void UpdateMe()
	{
		if (doMotionCheck) DoMotionCheck();

		if ( this != null && !Synchronize() )
		{
			MotionState = MotionStateEnum.Still;
		}
	}

	void DoMotionCheck()
	{
		motionCheckTime += Time.deltaTime;
		if (motionCheckTime > 5f)
		{
			motionCheckTime = 0f;
			doMotionCheck = false;
			if (MotionState == MotionStateEnum.Still)
			{
				base.OnDisable();
			}
		}
	}

	/// <summary>
	/// Essentially the Update loop
	/// </summary>
	/// <returns>true if transform changed</returns>
	private bool Synchronize()
	{
		if (!predictedState.Active)
		{
			return false;
		}

		bool server = isServer;
		if ( server && !serverState.Active ) {
			return false;
		}

		bool changed = false;

		if (IsFloatingClient)
		{
			changed &= CheckFloatingClient();
		}

		if (server)
		{
			changed &= CheckFloatingServer();
		}

		if (predictedState.Position != transform.localPosition)
		{
			Lerp();
			changed = true;
		}

		if (serverState.Position != serverLerpState.Position)
		{
			ServerLerp();
			changed = true;
		}

		if ( predictedState.SpinFactor != 0 ) {
			transform.Rotate( Vector3.forward, Time.deltaTime * predictedState.Speed * predictedState.SpinFactor );
			changed = true;
		}

		//Checking if we should change matrix once per tile
		if (server && registerTile.LocalPositionServer != Vector3Int.RoundToInt(serverState.Position) ) {
			CheckMatrixSwitch();
			registerTile.UpdatePositionServer();
			UpdateOccupant();
			changed = true;
		}
		//Registering
		if (registerTile.LocalPositionClient != Vector3Int.RoundToInt(predictedState.Position) )
		{
//			Logger.LogTraceFormat(  "registerTile updating {0}->{1} ", Category.Transform, registerTile.WorldPositionC, Vector3Int.RoundToInt( predictedState.WorldPosition ) );
			registerTile.UpdatePositionClient();
			changed = true;
		}

		return changed;
	}

	/// Manually set an item to a specific position. Use WorldPosition!
	[Server]
	public void SetPosition(Vector3 worldPos, bool notify = true, bool keepRotation = false)
	{
		if (worldPos != TransformState.HiddenPos && pushPull)
		{
			pushPull.parentContainer = null;
		}
		Poke();
		Vector2 pos = worldPos; //Cut z-axis
		serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ), true ).Id;
//		serverState.Speed = speed;
		serverState.WorldPosition = pos;
		if ( !keepRotation ) {
			serverState.SpinRotation = 0;
		}
		if (notify) {
			NotifyPlayers();
		}

		//Don't lerp (instantly change pos) if active state was changed
		if ( serverState.Speed > 0 ) {
			var preservedLerpPos = serverLerpState.WorldPosition;
			serverLerpState.MatrixId = serverState.MatrixId;
			serverLerpState.WorldPosition = preservedLerpPos;
		} else {
			serverLerpState = serverState;
		}
	}

	[Server]
	private void SyncMatrix() {
		if ( registerTile && !serverState.IsUninitialized) {
			registerTile.ServerSetNetworkedMatrixNetID(MatrixManager.Get( serverState.MatrixId ).NetID);
		}
	}

	[Server]
	public void CheckMatrixSwitch( bool notify = true ) {
		if ( IsFixedMatrix )
		{
			return;
		}


//		Logger.LogTraceFormat( "{0} doing matrix switch check for {1}", Category.Transform, gameObject.name, pos );
		var newMatrix = MatrixManager.AtPoint( serverState.WorldPosition.RoundToInt(), true );
		if ( serverState.MatrixId != newMatrix.Id ) {
			var oldMatrix = MatrixManager.Get( serverState.MatrixId );
			Logger.LogTraceFormat( "{0} matrix {1}->{2}", Category.Transform, gameObject, oldMatrix, newMatrix );

			if ( oldMatrix.IsMovable
			     && oldMatrix.MatrixMove.IsMovingServer )
			{
				Push( oldMatrix.MatrixMove.ServerState.FlyingDirection.Vector.To2Int(), oldMatrix.Speed );
				Logger.LogTraceFormat( "{0} inertia pushed while attempting matrix switch", Category.Transform, gameObject );
				return;
			}

			//It's very important to save World Pos before matrix switch and restore it back afterwards
			var preservedPos = serverState.WorldPosition;
			serverState.MatrixId = newMatrix.Id;
			serverState.WorldPosition = preservedPos;

			var preservedLerpPos = serverLerpState.WorldPosition;
			serverLerpState.MatrixId = serverState.MatrixId;
			serverLerpState.WorldPosition = preservedLerpPos;

			if ( notify ) {
				NotifyPlayers();
			}
		}
	}

	#region Hiding/Unhiding

	[Server]
	public void DisappearFromWorldServer(bool resetRotation = false)
	{
		OnPullInterrupt().Invoke();
		if (IsFloatingServer)
		{
			Stop(notify: false);
		}

		serverState.Position = TransformState.HiddenPos;
		serverLerpState.Position = TransformState.HiddenPos;

		if (resetRotation)
		{
			transform.localRotation = Quaternion.identity;
			//no spinning
			serverState.SpinFactor = 0;
			serverLerpState.SpinFactor = 0;
			serverState.SpinRotation = 0;
			serverLerpState.SpinRotation = 0;
		}

		NotifyPlayers();
		UpdateActiveStatusServer();
	}

	/// <summary>
	/// Make this object appear at the specified world position, with rotation matching the
	/// rotation of the matrix it appears in.
	/// </summary>
	/// <param name="worldPos">position to appear</param>
	[Server]
	public void AppearAtPositionServer(Vector3 worldPos)
	{
		SetPosition(worldPos);
		UpdateActiveStatusServer();
	}

	///     Convenience method to make stuff disappear at position.
	///     For CLIENT prediction purposes.
	public void DisappearFromWorld()
	{
		predictedState.Position = TransformState.HiddenPos;
		UpdateActiveStatusClient();
	}

	///     Convenience method to make stuff appear at position
	///     For CLIENT prediction purposes.
	public void AppearAtPosition(Vector3 worldPos)
	{
		var pos = (Vector2) worldPos; //Cut z-axis
		predictedState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ), false ).Id;
		predictedState.WorldPosition = pos;
		transform.position = pos;
		UpdateActiveStatusClient();
	}

	public void SetVisibleServer(bool visible)
    {
	    if ( visible )
	    {
			AppearAtPositionServer( PushPull.AssumedWorldPositionServer() );
	    }
	    else
	    {
			DisappearFromWorldServer();
	    }
    }

	/// Clientside
	/// Registers if unhidden, unregisters if hidden
	private void UpdateActiveStatusClient()
	{
		if (registerTile == null) return;
		if (predictedState.Active)
		{
			registerTile.UpdatePositionClient();
		}
		else
		{
			if ( registerTile ) {
				registerTile.UnregisterClient();
			}
		}
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = predictedState.Active;
		}
	}
	/// Serverside
	/// Registers if unhidden, unregisters if hidden
	private void UpdateActiveStatusServer()
	{
		if (serverState.Active)
		{
			registerTile.UpdatePositionServer();
		}
		else
		{
			if ( registerTile ) {
				registerTile.UnregisterServer();
			}
		}
		Collider2D[] colls = GetComponents<Collider2D>();
		foreach (var c in colls)
		{
			c.enabled = serverState.Active;
		}
	}
		#endregion

	/// Called from TransformStateMessage, applies state received from server to client
	public void UpdateClientState( TransformState newState ) {
		clientState = newState;

		OnUpdateRecieved().Invoke( Vector3Int.RoundToInt( newState.WorldPosition ) );

		//Ignore "Follow Updates" if you're pulling it
		if ( newState.Active
			&& newState.IsFollowUpdate
			&& pushPull && pushPull.IsPulledByClient( PlayerManager.LocalPlayerScript?.pushPull) )
		{
			return;
		}

		//We want to toggle the colls before moving the transform
		Collider2D[] colls = GetComponents<Collider2D>();
		foreach (var c in colls)
		{
			c.enabled = newState.Active;
		}

		//Don't lerp (instantly change pos) if active state was changed
		if (predictedState.Active != newState.Active /*|| newState.Speed == 0*/)
		{
			transform.position = newState.WorldPosition;
		}
		predictedState = newState;
		UpdateActiveStatusClient();
		//sync rotation if not spinning
		if ( predictedState.SpinFactor != 0 ) {
			return;
		}

		transform.localRotation = Quaternion.Euler( 0, 0, predictedState.SpinRotation );
	}

	//fire the server-side event hook and any additional logic that should run when tile is reached
	private void ServerOnTileReached(Vector3Int reachedWorldPosition)
	{
		OnTileReached().Invoke(reachedWorldPosition);
		UpdateOccupant();
	}

	/// <summary>
	/// Currently sending to everybody, but should be sent to nearby players only.
	///
	/// Notifies all players of rotation / position updates for this CNT.
	///
	/// </summary>
	[Server]
	public void NotifyPlayers()
	{
		//we check this == null to ensure this component hasn't been destroyed, sometimes
		//this can get called before an object has been fully destroyed
		if (this == null || gameObject == null) return; //sometimes between round restarts a call might be made on an object being destroyed

		//	Logger.LogFormat( "{0} Notified: {1}", Category.Transform, gameObject.name, serverState.WorldPosition );
		SyncMatrix();

		TransformStateMessage.SendToAll(gameObject, serverState);
	}

	/// <summary>
	/// Tell just one player about the new CNT position / rotation. Used to sync when a new player joins
	/// </summary>
	/// <param name="playerGameObject">Whom to notify</param>
	[Server]
	public void NotifyPlayer(NetworkConnection playerGameObject) {
		TransformStateMessage.Send(playerGameObject, gameObject, serverState);
	}

	// Checks if the object is occupiable and update occupant position if it's occupied (ex: a chair)
	[Server]
	private void UpdateOccupant()
	{
		if (occupiableDirectionalSprite != null && occupiableDirectionalSprite.HasOccupant)
		{
			if (occupiableDirectionalSprite.OccupantPlayerScript != null)
			{
				//sync position to ensure they buckle to the correct spot
				occupiableDirectionalSprite.OccupantPlayerScript.PlayerSync.SetPosition(registerTile.WorldPosition);
				Logger.LogTraceFormat("UpdatedOccupant {0}", Category.BuckledMovement, registerTile.WorldPosition);
			}
		}
	}
}

public class ThrowEvent : UnityEvent<ThrowInfo> {}
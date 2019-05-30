using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

// ReSharper disable CompareOfFloatsByEqualityOperator
/// Current state of transform, server modifies these and sends to clients.
/// Clients can modify these as well for prediction
public struct TransformState {
	public bool Active => Position != HiddenPos;
	///Don't set directly, use Speed instead.
	///public in order to be serialized :\
	public float speed;
	public float Speed {
		get { return speed; }
		set { speed = value < 0 ? 0 : value; }
	}

	///Direction of throw
	public Vector2 Impulse;

	/// Server-only active throw information
	[NonSerialized]
	public ThrowInfo ActiveThrow;

	public int MatrixId;

	/// Local position on current matrix
	public Vector3 Position;
	/// World position, more expensive to use
	public Vector3 WorldPosition
	{
		get
		{
			if ( !Active )
			{
				return HiddenPos;
			}

			return MatrixManager.LocalToWorld( Position, MatrixManager.Get( MatrixId ) );
		}
		set {
			if (value == HiddenPos) {
				Position = HiddenPos;
			}
			else
			{
				Position = MatrixManager.WorldToLocal( value, MatrixManager.Get( MatrixId ) );
			}
		}
	}

	/// Flag means that this update is a pull follow update,
	/// So that puller could ignore them
	public bool IsFollowUpdate;

	/// <summary>
	/// Degrees of rotation about the Z axis caused by spinning, using unity's convention for euler angles where positive = CCW.
	/// This is only used for spinning objects such as thrown ones or ones drifting in space and as such should not
	/// be used for determining actual facing. Only affects the transform.localRotation of an object (rotation relative
	/// to parent transform).
	/// </summary>
	public float SpinRotation;
	/// Spin direction and speed, if it should spin
	public sbyte SpinFactor;

	/// Means that this object is hidden
	public static readonly Vector3Int HiddenPos = new Vector3Int(0, 0, -100);
	/// Should only be used for uninitialized transform states, should NOT be used for anything else.
	public static readonly TransformState Uninitialized =
		new TransformState{ Position = HiddenPos, ActiveThrow = ThrowInfo.NoThrow, MatrixId = -1};


	/// <summary>
	/// Check if this represents the uninitialized state TransformState.Uninitialized
	/// </summary>
	/// <returns>true iff this is TransformState.Uninitialized</returns>
	public bool IsUninitialized => MatrixId == -1;

	public override string ToString()
	{
		if (Equals(Uninitialized))
		{
			return "[Uninitialized]";
		}
		else if (Position == HiddenPos)
		{
			return  $"[{nameof( Position )}: Hidden, {nameof( WorldPosition )}: Hidden, " +
			        $"{nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}, {nameof( SpinRotation )}: {SpinRotation}, " +
			        $"{nameof( SpinFactor )}: {SpinFactor}, {nameof( IsFollowUpdate )}: {IsFollowUpdate}, {nameof( MatrixId )}: {MatrixId}]";
		}
		else
		{
			return  $"[{nameof( Position )}: {(Vector2)Position}, {nameof( WorldPosition )}: {(Vector2)WorldPosition}, " +
			        $"{nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}, {nameof( SpinRotation )}: {SpinRotation}, " +
			        $"{nameof( SpinFactor )}: {SpinFactor}, {nameof( IsFollowUpdate )}: {IsFollowUpdate}, {nameof( MatrixId )}: {MatrixId}]";
		}

	}
}

public partial class CustomNetTransform : ManagedNetworkBehaviour, IPushable //see UpdateManager
{
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
			if ( ItemAttributes.size == 0 )
			{
				return ItemSize.Tiny;
			}
			return ItemAttributes.size;
		}
	}

	public Vector3Int ServerPosition => serverState.WorldPosition.RoundToInt();
	public Vector3Int ServerLocalPosition => serverState.Position.RoundToInt();
	public Vector3Int ClientPosition => predictedState.WorldPosition.RoundToInt();
	public Vector3Int ClientLocalPosition => predictedState.Position.RoundToInt();
	public Vector3Int TrustedPosition => clientState.WorldPosition.RoundToInt();
	public Vector3Int TrustedLocalPosition => clientState.Position.RoundToInt();

	/// <summary>
	/// Used to determine if this transform is worth updating every frame
	/// </summary>
	private enum MotionStateEnum { Moving, Still }

	private Coroutine limboHandle;
	private MotionStateEnum motionState = MotionStateEnum.Moving;
	/// <summary>
	/// Used to determine if this transform is worth updating every frame
	/// </summary>
	private MotionStateEnum MotionState
	{
		get { return motionState; }
		set
		{
			if ( motionState == value || UpdateManager.Instance == null )
			{
				return;
			}

			if ( value == MotionStateEnum.Moving )
			{
				base.OnEnable();
			}
			else
			{
				this.RestartCoroutine( FreezeWithTimeout(), ref limboHandle );
			}

			motionState = value;
		}
	}

	/// <summary>
	/// Waits 5 seconds and unsubscribes this CNT from Update() cycle
	/// </summary>
	private IEnumerator FreezeWithTimeout()
	{
		yield return WaitFor.Seconds(5);
		if ( MotionState == MotionStateEnum.Still )
		{
			base.OnDisable();
		}
	}

	private RegisterTile registerTile;
	public RegisterTile RegisterTile => registerTile;

	private ItemAttributes ItemAttributes {
		get {
			if ( itemAttributes == null ) {
				itemAttributes = GetComponent<ItemAttributes>();
			}
			return itemAttributes;
		}
	}
	private ItemAttributes itemAttributes;

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
		itemAttributes = GetComponent<ItemAttributes>();
		var _pushPull = PushPull; //init
		OnUpdateRecieved().AddListener( Poke );
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

		//If object is supposed to be hidden, keep it that way
//		var worldPos = serverState.WorldPosition;//
		serverState.Speed = 0;
		serverState.SpinRotation = transform.localRotation.eulerAngles.z;
		serverState.SpinFactor = 0;
		registerTile = GetComponent<RegisterTile>();

		//Matrix id init
		if ( registerTile && registerTile.Matrix ) {
			//pre-placed
			serverState.MatrixId = MatrixManager.Get( matrix ).Id;
			serverState.Position =
				Vector3Int.RoundToInt(new Vector3(transform.localPosition.x, transform.localPosition.y, 0));
		} else {
			//runtime-placed
			bool initError = !MatrixManager.Instance || !registerTile;
			if ( initError ) {
				serverState.MatrixId = 0;
				Logger.LogWarning( $"{gameObject.name}: unable to detect MatrixId!", Category.Transform );
			} else {
				serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt(transform.position), true ).Id;
			}
			serverState.WorldPosition = Vector3Int.RoundToInt((Vector2)transform.position);
		}

		serverLerpState = serverState;
	}

	/// Is it supposed to be hidden? (For init purposes)
	private bool IsHiddenOnInit {
		get {
			if ( Vector3Int.RoundToInt( transform.position ).Equals( TransformState.HiddenPos ) ||
			     Vector3Int.RoundToInt( transform.localPosition ).Equals( TransformState.HiddenPos ) ) {
				return true;
			}
			VisibleBehaviour visibleBehaviour = GetComponent<VisibleBehaviour>();
			return visibleBehaviour && !visibleBehaviour.visibleState;
		}
	}

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
		if ( !Synchronize() )
		{
			MotionState = MotionStateEnum.Still;
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
		if (server && registerTile.PositionServer != Vector3Int.RoundToInt(serverState.Position) ) {
			CheckMatrixSwitch();
			registerTile.UpdatePositionServer();
			changed = true;
		}
		//Registering
		if (registerTile.PositionClient != Vector3Int.RoundToInt(predictedState.Position) )
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
			registerTile.ParentNetId = MatrixManager.Get( serverState.MatrixId ).NetId;
		}
	}

	[Server]
	private void CheckMatrixSwitch( bool notify = true ) {
//		Logger.LogTraceFormat( "{0} doing matrix switch check for {1}", Category.Transform, gameObject.name, pos );
		int newMatrixId = MatrixManager.AtPoint( serverState.WorldPosition.RoundToInt(), true ).Id;
		if ( serverState.MatrixId != newMatrixId ) {
			Logger.LogTraceFormat( "{0} matrix {1}->{2}", Category.Transform, gameObject, serverState.MatrixId, newMatrixId );

			//It's very important to save World Pos before matrix switch and restore it back afterwards
			var preservedPos = serverState.WorldPosition;
			serverState.MatrixId = newMatrixId;
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
	public void DisappearFromWorldServer(bool stopInertia = true)
	{
		OnPullInterrupt().Invoke();
		serverState.Position = TransformState.HiddenPos;
		serverLerpState.Position = TransformState.HiddenPos;

		if (CheckFloatingServer() && stopInertia )
		{
			Stop();
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


	/// Clientside
	/// Registers if unhidden, unregisters if hidden
	private void UpdateActiveStatusClient()
	{
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
		//Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example.
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

		transform.localRotation = Quaternion.Euler( 0, 0, predictedState.SpinRotation );;
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
	//	Logger.LogFormat( "{0} Notified: {1}", Category.Transform, gameObject.name, serverState.WorldPosition );
		SyncMatrix();
		TransformStateMessage.SendToAll(gameObject, serverState);
	}

	/// <summary>
	/// Tell just one player about the new CNT position / rotation. Used to sync when a new player joins
	/// </summary>
	/// <param name="playerGameObject">Whom to notify</param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject) {
		TransformStateMessage.Send(playerGameObject, gameObject, serverState);
	}

	/// <summary>
	/// Invokes the OnSpawnedServer (on the server) and OnSpawnedClient (on each client) hooks so each component can
	/// initialize itself as / if needed
	/// </summary>
	[Server]
	public void FireSpawnHooks()
	{
		BroadcastMessage("OnSpawnedServer", SendMessageOptions.DontRequireReceiver);
		RpcFireSpawnHook();
	}

	[ClientRpc]
	private void RpcFireSpawnHook()
	{
		BroadcastMessage("OnSpawnedClient", SendMessageOptions.DontRequireReceiver);
	}

	/// <summary>
	/// Invokes the OnClonedServer (on the server) and OnClonedClient (on each client) hooks so each component can
	/// clone the specified object
	/// </summary>
	[Server]
	public void FireCloneHooks(GameObject clonedFrom)
	{
		BroadcastMessage("OnClonedServer", clonedFrom, SendMessageOptions.DontRequireReceiver);
		RpcFireCloneHook(clonedFrom);
	}

	[ClientRpc]
	private void RpcFireCloneHook(GameObject clonedFrom)
	{
		BroadcastMessage("OnClonedClient", clonedFrom, SendMessageOptions.DontRequireReceiver);
	}
}
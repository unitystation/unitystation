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

	public float Rotation;
	public bool IsLocalRotation;
	/// Spin direction and speed, if it should spin
	public sbyte SpinFactor;

	/// Means that this object is hidden
	public static readonly Vector3Int HiddenPos = new Vector3Int(0, 0, -100);
	/// Means that this object is hidden
	public static readonly TransformState HiddenState =
		new TransformState{ Position = HiddenPos, ActiveThrow = ThrowInfo.NoThrow, MatrixId = 0};

	public override string ToString()
	{
		return Equals( HiddenState ) ? "[Hidden]" : $"[{nameof( Position )}: {(Vector2)Position}, {nameof( WorldPosition )}: {(Vector2)WorldPosition}, " +
		       $"{nameof( Speed )}: {Speed}, {nameof( Impulse )}: {Impulse}, {nameof( Rotation )}: {Rotation}, {nameof( IsLocalRotation )}: {IsLocalRotation}, " +
		       $"{nameof( SpinFactor )}: {SpinFactor}, {nameof( IsFollowUpdate )}: {IsFollowUpdate}, {nameof( MatrixId )}: {MatrixId}]";
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
	private UnityEvent onPullInterrupt = new UnityEvent();
	public UnityEvent OnPullInterrupt() => onPullInterrupt;
	public Vector3Int ServerPosition => serverState.WorldPosition.RoundToInt();
	public Vector3Int ClientPosition => predictedState.WorldPosition.RoundToInt();
	public Vector3Int TrustedPosition => clientState.WorldPosition.RoundToInt();

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
		yield return YieldHelper.FiveSecs;
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

	private TransformState serverState = TransformState.HiddenState; //used for syncing with players, matters only for server
	private TransformState serverLerpState = TransformState.HiddenState; //used for simulating lerp on server

	private TransformState clientState = TransformState.HiddenState; //last reliable state from server
	private TransformState predictedState = TransformState.HiddenState; //client's transform, can get dirty/predictive

	private Matrix matrix => registerTile.Matrix;

	public TransformState ServerState => serverState;
	public TransformState ServerLerpState => serverLerpState;
	public TransformState PredictedState => predictedState;
	public TransformState ClientState => clientState;

	private void Start()
	{
		registerTile = GetComponent<RegisterTile>();
		itemAttributes = GetComponent<ItemAttributes>();
		tileDmgMask = LayerMask.GetMask ("Windows", "Walls");
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
		serverState.Rotation = transform.rotation.eulerAngles.z;
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
				serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt(transform.position) ).Id;
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
		if (server && registerTile.Position != Vector3Int.RoundToInt(serverState.Position) ) {
			CheckMatrixSwitch();
			RegisterObjects();
			changed = true;
		}
		//Registering
		if (!server && registerTile.Position != Vector3Int.RoundToInt(predictedState.Position) )
		{
			Logger.LogTraceFormat(  "registerTile updating {0}->{1} ", Category.Transform, registerTile.WorldPosition, Vector3Int.RoundToInt( predictedState.WorldPosition ) );
			RegisterObjects();
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
		serverState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
//		serverState.Speed = speed;
		serverState.WorldPosition = pos;
		if ( !keepRotation ) {
			serverState.Rotation = 0;
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
		if ( registerTile ) {
			registerTile.ParentNetId = MatrixManager.Get( serverState.MatrixId ).NetId;
		}
	}

	[Server]
	private void CheckMatrixSwitch( bool notify = true ) {
//		Logger.LogTraceFormat( "{0} doing matrix switch check for {1}", Category.Transform, gameObject.name, pos );
		int newMatrixId = MatrixManager.AtPoint( serverState.WorldPosition.RoundToInt() ).Id;
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
	public void DisappearFromWorldServer()
	{
		OnPullInterrupt().Invoke();
		serverState = TransformState.HiddenState;
		serverLerpState = TransformState.HiddenState;
		NotifyPlayers();
	}

	[Server]
	public void AppearAtPositionServer(Vector3 worldPos)
	{
		SetPosition(worldPos);
	}

	///     Convenience method to make stuff disappear at position.
	///     For CLIENT prediction purposes.
	public void DisappearFromWorld()
	{
		predictedState = TransformState.HiddenState;
		UpdateActiveStatus();
	}

	///     Convenience method to make stuff appear at position
	///     For CLIENT prediction purposes.
	public void AppearAtPosition(Vector3 worldPos)
	{
		var pos = (Vector2) worldPos; //Cut z-axis
		predictedState.MatrixId = MatrixManager.AtPoint( Vector3Int.RoundToInt( worldPos ) ).Id;
		predictedState.WorldPosition = pos;
		transform.position = pos;
		UpdateActiveStatus();
	}



	/// Registers if unhidden, unregisters if hidden
	private void UpdateActiveStatus()
	{
		if (predictedState.Active)
		{
			RegisterObjects();
		}
		else
		{
			if ( registerTile ) {
				registerTile.Unregister();
			}
		}
		//Consider moving VisibleBehaviour functionality to CNT. Currently VB doesn't allow predictive object hiding, for example.
		Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
		for (int i = 0; i < renderers.Length; i++)
		{
			renderers[i].enabled = predictedState.Active;
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
		UpdateActiveStatus();
		//sync rotation if not spinning
		if ( predictedState.SpinFactor != 0 ) {
			return;
		}

		var rotation = Quaternion.Euler( 0, 0, predictedState.Rotation );
		if ( predictedState.IsLocalRotation ) {
			transform.localRotation = rotation;
		} else {
			transform.rotation = rotation;
		}
	}

	///     Currently sending to everybody, but should be sent to nearby players only
	[Server]
	public void NotifyPlayers()
	{
	//	Logger.LogFormat( "{0} Notified: {1}", Category.Transform, gameObject.name, serverState.WorldPosition );
		SyncMatrix();
		serverState.IsLocalRotation = false;
		TransformStateMessage.SendToAll(gameObject, serverState);
	}

	///     Sync with new player joining
	/// <param name="playerGameObject">Whom to notify</param>
	/// <param name="isLocalRotation">(for init) tells client to assign transform.localRotation instead of transform.rotation if true</param>
	[Server]
	public void NotifyPlayer(GameObject playerGameObject, bool isLocalRotation = false) {
		serverState.IsLocalRotation = isLocalRotation;
		TransformStateMessage.Send(playerGameObject, gameObject, serverState);
	}

	/// Register item pos in matrix
	private void RegisterObjects() {
		registerTile.UpdatePosition();
	}
}
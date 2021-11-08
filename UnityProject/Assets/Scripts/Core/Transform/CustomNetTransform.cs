using System;
using System.Collections;
using Items;
using UnityEngine;
using UnityEngine.Events;
using Mirror;
using NaughtyAttributes;
using Objects;

// ReSharper disable CompareOfFloatsByEqualityOperator

public partial class CustomNetTransform : NetworkBehaviour, IPushable
{
	[SerializeField][Tooltip("When the scene loads, snap this to the middle of the nearest tile?")]
	private bool snapToGridOnStart = true;
	public bool SnapToGridOnStart => snapToGridOnStart;

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
	public Vector3Int ServerLocalPosition => serverState.LocalPosition.RoundToInt();
	public Vector3Int ClientPosition => predictedState.WorldPosition.RoundToInt();
	public Vector3Int ClientLocalPosition => predictedState.LocalPosition.RoundToInt();
	public Vector3Int TrustedPosition => clientState.WorldPosition.RoundToInt();
	public Vector3Int TrustedLocalPosition => clientState.LocalPosition.RoundToInt();
	public Vector3Int LastNonHiddenPosition { get; } = TransformState.HiddenPos; //todo: implement for CNT!

	//Timer to unsubscribe from the UpdateManager if the object is still for too long
	private float stillTimer;

	private bool isUpdating;

	private bool Initialized;

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

	[ReadOnlyAttribute] private TransformState serverState = TransformState.Uninitialized; //used for syncing with players, matters only for server
	[ReadOnlyAttribute] private TransformState serverLerpState = TransformState.Uninitialized; //used for simulating lerp on server

	[ReadOnlyAttribute] private TransformState clientState = TransformState.Uninitialized; //last reliable state from server

	#region ClientStateSyncVars
	// ClientState SyncVars, separated out of clientState TransformState
	// So we only send the relevant data not all values each time, to reduce network usage

	[SyncVar(hook = nameof(SyncClientStateSpeed))]
	private float clientStateSpeed;

	[SyncVar(hook = nameof(SyncClientStateWorldImpulse))]
	private Vector2 clientStateWorldImpulse;

	[SyncVar(hook = nameof(SyncClientStateMatrixId))]
	private int clientStateMatrixId = -1;

	[SyncVar(hook = nameof(SyncClientStateLocalPosition))]
	private Vector3 clientStateLocalPosition = TransformState.HiddenPos;

	[SyncVar(hook = nameof(SyncClientStateIsFollowUpdate))]
	private bool clientStateIsFollowUpdate;

	[SyncVar(hook = nameof(SyncClientStateSpinRotation))]
	private float clientStateSpinRotation;

	[SyncVar(hook = nameof(SyncClientStateSpinFactor))]
	private sbyte clientStateSpinFactor;

	private bool clientValueChanged;

	#endregion

	[ReadOnlyAttribute] private TransformState predictedState = TransformState.Uninitialized; //client's transform, can get dirty/predictive

	private Matrix matrix => registerTile.Matrix;

	public TransformState ServerState => serverState;
	public TransformState ServerLerpState => serverLerpState;
	public TransformState PredictedState => predictedState;
	public TransformState ClientState => clientState;

	private bool waitForId;
	private bool WaitForMatrixId;

	private void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		itemAttributes = GetComponent<ItemAttributesV2>();
		occupiableDirectionalSprite = GetComponent<OccupiableDirectionalSprite>();
		syncInterval = 0f;
	}

	private void Start()
	{
		OnUpdateRecieved().AddListener(Poke);
	}

	public void OnDisable()
	{
		UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
	}

	public void SetInitialPositionStates()
	{
		if (transform.position.z != -100) //mapping mistakes correction
		{
			transform.position = new Vector2(transform.position.x, transform.position.y);
		}

		var pos = transform.position;
		if (snapToGridOnStart)
		{
			pos = pos.RoundToInt();
		}
		var matrixInfo = matrix.MatrixInfo;

		predictedState.MatrixId = matrixInfo.Id;
		predictedState.WorldPosition = pos;
		serverState.MatrixId = matrixInfo.Id;
		serverState.WorldPosition = pos;
		clientState.MatrixId = matrixInfo.Id;
		clientState.WorldPosition = pos;
		serverLerpState.MatrixId = matrixInfo.Id;
		serverLerpState.WorldPosition = pos;

		if (CustomNetworkManager.IsServer)
		{
			InitServerState();
		}

		if (pos == TransformState.HiddenPos)
		{
			Collider2D[] colls = GetComponents<Collider2D>();
			foreach (var c in colls)
			{
				c.enabled = false;
			}
		}

		Initialized = true;
	}

	[Server]
	private void InitServerState()
	{
		if (Vector3Int.RoundToInt(transform.position).Equals(TransformState.HiddenPos)
		    || Vector3Int.RoundToInt(transform.localPosition).Equals(TransformState.HiddenPos))
		{
			return;
		}

		//If object is supposed to be hidden, keep it that way
		serverState.Speed = 0;
		serverState.SpinRotation = transform.localRotation.eulerAngles.z;
		serverState.SpinFactor = 0;

		registerTile.UpdatePositionServer();
		NotifyPlayers();
	}

		//Server and Client Side
	private void UpdateMe()
	{
		if (Synchronize())
		{
			stillTimer = 0;
		}
		else
		{
			stillTimer += Time.deltaTime;
			if (stillTimer >= 5)
			{
				UpdateManager.Remove(CallbackType.UPDATE, UpdateMe);
				isUpdating = false;
			}
		}
	}

	/// <summary>
	/// Essentially the Update loop
	/// </summary>
	/// <returns>true if transform changed</returns>
	private bool Synchronize()
	{
		if (this == null)
		{
			//Poke() can be hit in the middle of roundend/roundstart transition while our OnDisable() has already ran
			//In that case the UpdateManager will carry a reference to the action from a deleted gameobject
			//TODO: fix initialization order t
			return false;
		}
		//Isn't run on server as clientValueChanged is always false for server
		//Pokes the client to do changes if values have changed from syncvars
		if (clientValueChanged)
		{
			clientValueChanged = false;
			PerformClientStateUpdate(clientState, clientState);
		}

		if (!predictedState.Active)
		{
			return false;
		}

		bool server = CustomNetworkManager.IsServer;
		if (server && !serverState.Active)
		{
			return false;
		}

		bool changed = false;

		//Apparently needs to run on server or else items will spin around forever
		//might need looking into so the server isn't doing the floating and other matrix checks twice
		if (IsFloatingClient)
		{
			changed &= CheckFloatingClient();
		}

		if (server && IsFloatingServer)
		{
			changed &= CheckFloatingServer();
		}

		if ((Vector2)predictedState.LocalPosition != (Vector2)transform.localPosition)
		{
			Lerp();
			changed = true;
		}

		if ((Vector2)serverState.WorldPosition != (Vector2)serverLerpState.WorldPosition)
		{
			ServerLerp();
			changed = true;
		}

		if ( predictedState.SpinFactor != 0 ) {
			transform.Rotate( Vector3.forward, Time.deltaTime * predictedState.Speed * predictedState.SpinFactor );
			changed = true;
		}

		//Checking if we should change matrix once per tile
		if (server && registerTile.LocalPositionServer != Vector3Int.RoundToInt(serverState.LocalPosition) ) {
			CheckMatrixSwitch();
			registerTile.UpdatePositionServer();
			UpdateOccupant();
			changed = true;
		}
		//Registering
		if (registerTile.LocalPositionClient != Vector3Int.RoundToInt(predictedState.LocalPosition) )
		{
			if (server)
			{
				if (registerTile.ServerSetNetworkedMatrixNetID(MatrixManager.Get(predictedState.MatrixId).NetID) == false)
				{
					registerTile.UpdatePositionClient();
				}
			}
			else
			{
				registerTile.UpdatePositionClient();
			}

			changed = true;
		}

		return changed;
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
	private void Poke(Vector3Int v)
	{
		if (isUpdating == false && Initialized)
		{
			UpdateManager.Add(CallbackType.UPDATE, UpdateMe);
			isUpdating = true;
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
		serverState.MatrixId = MatrixManager.AtPoint(Vector3Int.RoundToInt(worldPos), true).Id;
		if (!keepRotation)
		{
			serverState.SpinRotation = 0;
		}

		if (serverState.Speed > 0)
		{
			serverLerpState.MatrixId = serverState.MatrixId;
			serverLerpState.WorldPosition = serverState.WorldPosition;
			serverState.WorldPosition = pos;
		}
		else
		{
			serverLerpState = serverState; //Don't lerp (instantly change pos) if active state was changed
		}
		serverState.WorldPosition = pos;

		if (notify)
		{
			NotifyPlayers();
		}
	}

	[Server]
	private void SyncMatrix()
	{
		if (registerTile && !serverState.IsUninitialized)
		{
			registerTile.ServerSetNetworkedMatrixNetID(MatrixManager.Get(serverState.MatrixId).NetID);
		}
	}

	[Server]
	public void CheckMatrixSwitch( bool notify = true ) {
		if ( IsFixedMatrix )
		{
			return;
		}


//		Logger.LogTraceFormat( "{0} doing matrix switch check for {1}", Category.Matrix, gameObject.name, pos );
		var newMatrix = MatrixManager.AtPoint( serverState.WorldPosition.RoundToInt(), true );
		if ( serverState.MatrixId != newMatrix.Id ) {
			var oldMatrix = MatrixManager.Get( serverState.MatrixId );
			Logger.LogTraceFormat( "{0} matrix {1}->{2}", Category.Matrix, gameObject, oldMatrix, newMatrix );

			if ( oldMatrix.IsMovable
			     && oldMatrix.MatrixMove.IsMovingServer )
			{
				Push( oldMatrix.MatrixMove.ServerState.FlyingDirection.Vector.To2Int(), oldMatrix.Speed );
				Logger.LogTraceFormat( "{0} inertia pushed while attempting matrix switch", Category.Matrix, gameObject );
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

		serverState.LocalPosition = TransformState.HiddenPos;
		serverLerpState.LocalPosition = TransformState.HiddenPos;

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
		predictedState.LocalPosition = TransformState.HiddenPos;
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
	    if (visible)
	    {
			var objectBehaviour = PushPull.TopContainer;

			if (objectBehaviour.transform.position == TransformState.HiddenPos)
			{
				Logger.LogError($"${this} is set to become visible at HiddenPos!");
			}

			AppearAtPositionServer(objectBehaviour.AssumedWorldPositionServer());
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
			if (!registerTile) registerTile = this.GetComponent<RegisterTile>();
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

	#region ClientState SyncMethods

	[Client]
	public void SyncClientStateSpeed(float oldSpeed, float newSpeed)
	{
		clientStateSpeed = newSpeed;
		clientState.Speed = newSpeed;
		ClientValueChanged();
	}

	[Client]
	public void SyncClientStateWorldImpulse(Vector2 oldWorldImpulse, Vector2 newWorldImpulse)
	{
		clientStateWorldImpulse = newWorldImpulse;
		clientState.WorldImpulse = newWorldImpulse;
		ClientValueChanged();
	}

	[Client]
	public void SyncClientStateMatrixId(int oldMatrixId, int newMatrixId)
	{
		clientStateMatrixId = newMatrixId;
		clientState.MatrixId = newMatrixId;
		ClientValueChanged();
	}

	[Client]
	public void SyncClientStateLocalPosition(Vector3 oldLocalPosition, Vector3 newLocalPosition)
	{
		clientStateLocalPosition = newLocalPosition;
		clientState.LocalPosition = newLocalPosition;
		ClientValueChanged();
	}

	[Client]
	public void SyncClientStateIsFollowUpdate(bool oldIsFollowUpdate, bool newIsFollowUpdate)
	{
		clientStateIsFollowUpdate = newIsFollowUpdate;
		clientState.IsFollowUpdate = newIsFollowUpdate;
		ClientValueChanged();
	}

	[Client]
	public void SyncClientStateSpinRotation(float oldSpinRotation, float newSpinRotation)
	{
		clientStateSpinRotation = newSpinRotation;
		clientState.SpinRotation = newSpinRotation;
		ClientValueChanged();
	}

	[Client]
	public void SyncClientStateSpinFactor(sbyte oldSpinFactor, sbyte newSpinFactor)
	{
		clientStateSpinFactor = newSpinFactor;
		clientState.SpinFactor = newSpinFactor;
		ClientValueChanged();
	}

	[Client]
	private void ClientValueChanged()
	{
		Poke();
		clientValueChanged = true;
	}

	#endregion

	[Server]
	private void UpdateClientState(TransformState oldState, TransformState newState)
	{
		clientStateSpeed = newState.speed;
		clientStateWorldImpulse = newState.WorldImpulse;
		clientStateMatrixId = newState.MatrixId;
		clientStateLocalPosition = newState.LocalPosition;
		clientStateIsFollowUpdate = newState.IsFollowUpdate;
		clientStateSpinRotation = newState.SpinRotation;
		clientStateSpinFactor = newState.SpinFactor;

		clientState = newState;
		PerformClientStateUpdate(oldState, newState);
	}

	/// Called from TransformStateMessage, applies state received from server to client
	private void PerformClientStateUpdate(TransformState oldState, TransformState newState)
	{
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
		if (predictedState.Active != newState.Active || newState.Active == false /*|| newState.Speed == 0*/)
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

		//Wait for this components id
		if (TryGetComponent<NetworkIdentity>(out var networkIdentity))
		{
			if (networkIdentity.netId == 0)
			{
				//netIds default to 0 when spawned, a new Id is assigned but this happens a bit later
				//this is just to catch multiple 0's
				//An identity could have a valid id of 0, but since this message is only for net transforms and since the
				//identities on the managers will get set first, this shouldn't cause any issues.

				if (waitForId == false)
				{
					waitForId = true;
					StartCoroutine(IdWait(networkIdentity));
				}

				return;
			}
		}

		//Wait for networked matrix id to init
		if (serverState.IsUninitialized || matrix.NetworkedMatrix == null || matrix.NetworkedMatrix.MatrixSync.netId == 0)
		{
			if (WaitForMatrixId == false)
			{
				WaitForMatrixId = true;
				StartCoroutine(NetworkedMatrixIdWait());
			}

			return;
		}

		SyncMatrix();

		UpdateClientState(clientState, serverState);
	}

	private IEnumerator IdWait(NetworkIdentity net)
	{
		while (net.netId == 0)
		{
			yield return WaitFor.EndOfFrame;
		}

		waitForId = false;

		NotifyPlayers();
	}

	private IEnumerator NetworkedMatrixIdWait()
	{
		var networkMatrix = matrix.NetworkedMatrix;

		if (networkMatrix == null)
		{
			Logger.LogError($"networkMatrix was null on {matrix.gameObject.name}", Category.Matrix);
			WaitForMatrixId = false;
			yield break;
		}

		var matrixSync = networkMatrix.MatrixSync;

		if(matrixSync == null)
		{
			networkMatrix.BackUpSetMatrixSync();
			matrixSync = networkMatrix.MatrixSync;

			if (matrixSync == null)
			{
				//Theres a log in BackUpSetMatrixSync which will trigger on fail
				WaitForMatrixId = false;
				yield break;
			}
		}

		while (matrixSync.netId == 0)
		{
			yield return new WaitForSeconds(0.1f);
		}

		WaitForMatrixId = false;

		SyncMatrix();

		UpdateClientState(clientState, serverState);
	}

	/// <summary>
	/// Tell just one player about the new CNT position / rotation. Used to sync when a new player joins
	/// </summary>
	/// <param name="playerGameObject">Whom to notify</param>
	[Server]
	public void NotifyPlayer(NetworkConnection playerGameObject)
	{
		UpdateClientState(clientState, serverState);
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
				Logger.LogTraceFormat("UpdatedOccupant {0}", Category.Movement, registerTile.WorldPosition);
			}
		}
	}
}

public class ThrowEvent : UnityEvent<ThrowInfo> {}

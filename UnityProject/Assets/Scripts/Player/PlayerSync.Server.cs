using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public partial class PlayerSync
{
	//Server-only fields, don't concern clients in any way
	private DualVector3IntEvent onStartMove = new DualVector3IntEvent();
	public DualVector3IntEvent OnStartMove() => onStartMove;
	private Vector3IntEvent onTileReached = new Vector3IntEvent();
	public Vector3IntEvent OnTileReached() => onTileReached;
	private Vector3IntEvent onUpdateReceived = new Vector3IntEvent();
	public Vector3IntEvent OnUpdateRecieved() => onUpdateReceived;
	private UnityEvent onPullInterrupt = new UnityEvent();
	public UnityEvent OnPullInterrupt() => onPullInterrupt;
	public CollisionEvent onHighSpeedCollision = new CollisionEvent();
	public CollisionEvent OnHighSpeedCollision() => onHighSpeedCollision;

	public Vector3Int ServerPosition => serverState.WorldPosition.RoundToInt();
	public Vector3Int ServerLocalPosition => serverState.Position.RoundToInt();

	public Vector3Int LastNonHiddenPosition => serverState.LastNonHiddenPosition.RoundToInt();

	/// Current server state. Integer positions.
	private PlayerState serverState;

	/// Serverside lerping state that simulates where players should be on clients at the moment.
	/// Tracks in-between integer positions unlike serverState. Basically when position changes, it starts with
	/// setting serverState to the new position, then serverLerpState lerps from the current position to
	/// serverState until it reaches it.
	private PlayerState serverLerpState;

	private Queue<PlayerAction> serverPendingActions;

	/// Max size of serverside queue, client will be rolled back if it overflows
	private readonly int maxServerQueue = 10;

	private HashSet<PushPull> questionablePushables = new HashSet<PushPull>();

	/// Last direction that player moved in. Currently works more like a true impulse, therefore is zero-able
	private Vector2 lastDirectionServer;

	private RegisterPlayer registerPlayer;

	public float SpeedServer
	{
		get => masterSpeedServer;
		set
		{ // Future move speed (applied on the next step)
			if ( Math.Abs( masterSpeedServer - value ) > 0.01f )
			{
				Logger.LogTraceFormat( "{0}: setting SERVER speed {1}->{2}", Category.Movement, gameObject.name, SpeedServer, value );
			}
			masterSpeedServer = value < 0 ? 0 : value;
		}
	}
	/// <summary>
	/// Player's serverside move speed, applied on tile change
	/// </summary>
	private float masterSpeedServer;

	///
	public bool IsWeightlessServer {
		get {
			if (playerScript.IsGhost)
			{
				return false;
			}
			GameObject[] context = pushPull.IsPullingSomethingServer ? new[]{gameObject, pushPull.PulledObjectServer.gameObject} : new[]{gameObject};
			return MatrixManager.IsFloatingAt( context, Vector3Int.RoundToInt( serverState.WorldPosition ), isServer: true );
		}
	}

	/// <summary>
	/// If the position of this player is "non-sticky", i.e. meaning they would slide / float in a given direction
	/// </summary>
	public bool IsNonStickyServer
	{
		get
		{
			if (registerPlayer.IsSlippingServer)
			{
				return MatrixManager.IsNoGravityAt(serverState.WorldPosition.RoundToInt(), true)
				    || MatrixManager.IsSlipperyAt(serverState.WorldPosition.RoundToInt());
			}
			return !playerScript.IsGhost && MatrixManager.IsNonStickyAt(serverState.WorldPosition.RoundToInt(), true);
		}
	}

	public bool CanNotSpaceMoveServer => IsWeightlessServer && !IsAroundPushables( serverState, true );


	public bool IsMovingServer => consideredFloatingServer || !ServerPositionsMatch;
	public Vector2 ServerImpulse => serverState.WorldImpulse;

	/// Whether player is considered to be floating on server
	private bool consideredFloatingServer => serverState.WorldImpulse != Vector2.zero /*&& !IsBeingPulledServer*/;

	/// Do current and target server positions match?
	private bool ServerPositionsMatch => serverState.WorldPosition == serverLerpState.WorldPosition;


	///
		[Server]
		private void InitServerState()
		{
			Vector3Int worldPos = Vector3Int.RoundToInt((Vector2) transform.position); //cutting off Z-axis & rounding
			MatrixInfo matrixAtPoint = MatrixManager.AtPoint(worldPos, true);
			masterSpeedServer = playerMove.RunSpeed;
			PlayerState state = new PlayerState
			{
				MoveNumber = 0,
				MatrixId = matrixAtPoint.Id,
				WorldPosition = worldPos,
				Speed = masterSpeedServer
			};
			Logger.LogTraceFormat( "{0}: InitServerState for {1} found matrix {2} resulting in\n{3}", Category.Movement,
				PlayerList.Instance.Get( gameObject ).Name, worldPos, matrixAtPoint, state );
			serverLerpState = state;
			ServerState = state;
	}

	private PlayerAction lastAddedAction = PlayerAction.None;
	private Coroutine floatingSyncHandle;

	[Command]
	private void CmdProcessAction(PlayerAction action)
	{
		if ( serverPendingActions.Count > 0 && !lastAddedAction.Equals(PlayerAction.None)
		     && lastAddedAction.isNonPredictive && action.isNonPredictive )
		{
			Logger.Log( $"Ignored {action}: two non-predictive actions in a row!", Category.Movement );
			return;
		}

		//add action to server simulation queue
		serverPendingActions.Enqueue(action);

		lastAddedAction = action;


		//Rollback pos and punish player if server queue size is more than max size
		if (serverPendingActions.Count > maxServerQueue)
		{
			RollbackPosition();
			Logger.LogWarning( $"{gameObject.name}: Server pending actions overflow! (More than {maxServerQueue})." +
			                   "\nEither server lagged or player is attempting speedhack", Category.Movement );
		}
	}

	public void SetVisibleServer(bool visible)
	{
		if (!isServer) return;

		if ( visible )
		{
			AppearAtPositionServer( pushPull.AssumedWorldPositionServer() );
		}
		else
		{
			DisappearFromWorldServer();
		}
	}
	public void NewtonianMove(Vector2Int direction, float speed = Single.NaN)
	{
		//if we are buckled, transfer the impulse to our buckled object.
		if (playerMove.IsBuckled)
		{
			var buckledCNT = playerMove.BuckledObject.GetComponent<CustomNetTransform>();
			buckledCNT.NewtonianMove(direction, speed);
		}
		else
		{
			PushInternal(direction, true, speed);
		}
	}

	/// <summary>
	/// Push player in direction.
	/// Impulse should be consumed after one tile if indoors,
	/// and last indefinitely (until hit by obstacle) if you pushed someone into deep space
	/// </summary>
	/// <param name="direction"></param>
	/// <param name="speed"></param>
	/// <param name="followMode">used when object is following its puller
	/// (turns on tile snapping and removes player collision check)</param>
	/// <returns>true if push was successful</returns>
	[Server]
	public bool Push(Vector2Int direction, float speed = Single.NaN, bool followMode = false, bool ignorePassable = false)
	{
		//if we are buckled, transfer the impulse to our buckled object.
		if (playerMove.IsBuckled)
		{
			var buckledCNT = playerMove.BuckledObject.GetComponent<CustomNetTransform>();
			return buckledCNT.Push(direction, speed, followMode);
		}
		else
		{
			return PushInternal(direction, false, speed, followMode, ignorePassable);
		}
	}

	private bool PushInternal(
			Vector2Int direction, bool isNewtonian = false, float speed = Single.NaN, bool followMode = false, bool ignorePassable = false)
	{
		if (!float.IsNaN(speed) && speed <= 0)
		{
			return false;
		}

		speed = float.IsNaN( speed ) ? PushPull.DEFAULT_PUSH_SPEED : speed;

		direction = direction.Normalize();

		if (direction == Vector2Int.zero)
		{
			return false;
		}

		Vector3Int origin = ServerPosition;
		Vector3Int pushGoal = origin + direction.To3Int();

		if (!ignorePassable && !MatrixManager.IsPassableAt( origin, pushGoal, isServer: true, includingPlayers: !followMode ) ) {
			return false;
		}

		float uncorrectedSpeed = speed;

		if (isNewtonian)
		{
			if (!MatrixManager.IsSlipperyOrNoGravityAt(pushGoal))
			{
				return false;
			}

			if (consideredFloatingServer)
			{
				var currentFlyingDirection = serverState.WorldImpulse;
				var currentFlyingSpeed = serverState.speed;

				float correctedSpeed = speed;

				bool isOppositeDirection = direction == -currentFlyingDirection;
				bool isSameDirection = direction == currentFlyingDirection;

				if (isOppositeDirection)
				{
					Logger.LogTrace("got counter impulse, stopping", Category.PushPull);
					Stop();
					return true;
				}

				if (isSameDirection)
				{
					correctedSpeed = Mathf.Clamp(speed + currentFlyingSpeed, currentFlyingSpeed, PushPull.MAX_NEWTONIAN_SPEED);
				}

				Logger.LogTraceFormat("proposed: {0}@{1}, current: {2}@{3}, result: {4}@{5}", Category.PushPull,
					direction, speed, currentFlyingDirection, currentFlyingSpeed, direction, correctedSpeed
					);
				speed = correctedSpeed;
			}
		}

		if ( followMode ) {
			playerDirectional.FaceDirection(Orientation.From(direction));
			//force directional update of client, since it can't predict where it's being pulled
			var conn = playerScript.connectionToClient;
			if (conn != null)
			{
				playerDirectional.TargetForceSyncDirection(conn);
			}

		}
		else if ( uncorrectedSpeed >= playerMove.PushFallSpeed )
		{
			registerPlayer.ServerSlip(true);
		}

		Logger.LogTraceFormat( "{1}: Server push to {0}", Category.PushPull, pushGoal, gameObject.name );
		ClearQueueServer();
		MatrixInfo newMatrix = MatrixManager.AtPoint( pushGoal, true );
		//Note the client queue reset
		var newState = new PlayerState {
			MoveNumber = 0,
			WorldImpulse = direction,
			MatrixId = newMatrix.Id,
			WorldPosition = pushGoal,
			ImportantFlightUpdate = true,
			ResetClientQueue = true,
			IsFollowUpdate = followMode,
			Speed = speed
		};
		lastDirectionServer = direction;
		ServerState = newState;
		SyncMatrix();
		OnStartMove().Invoke( origin, pushGoal );
		NotifyPlayers();

		return true;
	}

	/// Manually set player to a specific world position.
	/// Also clears prediction queues.
	/// <param name="worldPos">The new position to "teleport" player</param>
	[Server]
	public void SetPosition(Vector3 worldPos, bool noLerp = false)
	{
		ClearQueueServer();
		Vector3Int roundedPos = Vector3Int.RoundToInt((Vector2)worldPos); //cutting off z-axis
		MatrixInfo newMatrix = MatrixManager.AtPoint(roundedPos, true);
		//Note the client queue reset
		var newState = new PlayerState
		{
			MoveNumber = 0,
			MatrixId = newMatrix.Id,
			WorldPosition = roundedPos,
			ResetClientQueue = true,
			Speed = SpeedServer
		};
		serverLerpState = newState;
		ServerState = newState;
		SyncMatrix();
		NotifyPlayers(noLerp);
		registerPlayer.UpdatePositionServer();
	}

	///	When lerp is finished, inform players of new state
	[Server]
	private bool TryNotifyPlayers()
	{
		if ( !ServerPositionsMatch ) {
			return false;
		}

//			Logger.LogTrace( $"{gameObject.name}: PSync Notify success!", Category.Movement );
		serverLerpState = serverState;
		SyncMatrix();
		NotifyPlayers();
//			TryUpdateServerTarget();
		return true;
	}

	/// Register player to matrix from serverState (ParentNetId is a SyncVar)
	[Server]
	private void SyncMatrix()
	{
		registerPlayer.ServerSetNetworkedMatrixNetID(MatrixManager.Get(serverState.MatrixId).NetID);
	}

	/// Send current serverState to just one player
	/// <param name="recipient">whom to inform</param>
	/// <param name="noLerp">(for init) tells client to do no lerping when changing pos this time</param>
	[Server]
	public void NotifyPlayer(NetworkConnection recipient, bool noLerp = false)
	{
		serverState.NoLerp = noLerp;
		var msg = PlayerMoveMessage.Send(recipient, gameObject, serverState);
		Logger.LogTraceFormat("Sent {0}", Category.Movement, msg);
	}

	/// Send current serverState to all players
	[Server]
	public void NotifyPlayers() {
		NotifyPlayers(false);
	}

	/// Send current serverState to all players
	[Server]
	public void NotifyPlayers(bool noLerp)
	{
		//Generally not sending mid-flight updates (unless there's a sudden change of course etc.)
		if (!serverState.ImportantFlightUpdate && consideredFloatingServer)
		{
			return;
		}

		//hack to make clients accept non-pull-breaking external pushes for stuff they're pulling
		//because they ignore updates for stuff they pull w/prediction
		bool isPullNudge = pushPull
						&& pushPull.IsBeingPulled
						&& !serverState.IsFollowUpdate
						&& serverState.WorldImpulse != Vector2.zero;
		if ( isPullNudge ) {
			//temporarily "break" pull so that puller would get the update
			InformPullMessage.Send( pushPull.PulledBy, this.pushPull, null );
		}

		serverState.NoLerp = noLerp;
		PlayerMoveMessage.SendToAll(gameObject, serverState);
//		Logger.LogTraceFormat("SentToAll {0}", Category.Movement, msg);
		//Clearing state flags
		serverState.ImportantFlightUpdate = false;
		serverState.ResetClientQueue = false;

		if ( isPullNudge ) {
			//restore pull for client.
			//previous fake break erases all pull train info from train head, so we make head aware again
			pushPull.InformHead( pushPull.PulledBy );
//			InformPullMessage.Send( pushPull.PulledBy, this.pushPull, pushPull.PulledBy );
		}
	}

	/// Clears server pending actions queue
	private void ClearQueueServer()
	{
		//			Logger.Log("Server queue wiped!");
		if (serverPendingActions != null && serverPendingActions.Count > 0)
		{
			serverPendingActions.Clear();
		}
	}

	/// Clear all queues and
	/// inform players of true serverState
	[Server]
	private void RollbackPosition()
	{
		foreach ( var questionablePushable in questionablePushables ) {
			//make sure component is not already destroyed
			if (questionablePushable == null) continue;
			Logger.LogWarningFormat( "Notified questionable pushable {0}", Category.PushPull, questionablePushable );
			questionablePushable.NotifyPlayers();
		}
		SetPosition(serverState.WorldPosition);
		questionablePushables.Clear();
	}

	/// Tries to assign next target from queue to serverTargetState if there are any
	/// (In order to start lerping towards it)
	[Server]
	private void TryUpdateServerTarget()
	{
		if (serverPendingActions.Count == 0)
		{
			return;
		}
		if ( consideredFloatingServer || !serverState.Active || CanNotSpaceMoveServer || (pushPull && pushPull.IsBeingPulled) )
		{
			Logger.LogWarning("Server ignored queued move while player isn't supposed to move", Category.Movement);
			serverPendingActions.Dequeue();

			TryUpdateServerTarget();
			return;
		}

		var curState = serverState;
		PlayerState nextState = NextStateServer( curState, serverPendingActions.Dequeue() );

		if ( Equals( curState, nextState ) )
		{
			TryUpdateServerTarget();
			return;
		}

		var newPos = nextState.WorldPosition;
		var oldPos = serverState.WorldPosition;
		lastDirectionServer = Vector2Int.RoundToInt(newPos - oldPos);
		ServerState = nextState;
		//In case positions already match
		TryNotifyPlayers();
		if ( lastDirectionServer != Vector2.zero ) {
			CheckMovementServer();
			OnStartMove().Invoke( oldPos.RoundToInt(), newPos.RoundToInt() );
		}

		TryUpdateServerTarget();
		//Logger.Log($"Server Updated target {serverTargetState}. {serverPendingActions.Count} pending");
	}

	/// Main server movement processing / validation logic.
	[Server]
	private PlayerState NextStateServer(PlayerState state, PlayerAction action)
	{
		//movement not allowed when buckled
		if (playerMove.IsBuckled)
		{
			Logger.LogWarning( $"Ignored {action}: player is bucked, rolling back!", Category.Movement );
			RollbackPosition();
			return state;
		}

		//Check if there is a bump interaction according to the server
		BumpType serverBump = CheckSlideAndBump(state, isServer: true, ref action);

		//Client only needs to check whether movement was prevented, specific type of bump doesn't matter
		bool isClientBump = action.isBump;

		if ( !playerScript.playerHealth || !playerScript.playerHealth.IsSoftCrit )
		{
			SpeedServer = action.isRun ? playerMove.RunSpeed : playerMove.WalkSpeed;
		}

		//we only lerp back if the client thinks it's passable  but server does not...if client
		//thinks it's not passable and server thinks it's passable, then it's okay to let the client continue
		if (!isClientBump && serverBump != BumpType.None && serverBump != BumpType.Swappable) {
			Logger.LogWarningFormat( "isBump mismatch, resetting: C={0} S={1}", Category.Movement, isClientBump, serverBump != BumpType.None );
			RollbackPosition();
			//laggy client may have predicted a swap with another player,
			//in which case they must also roll back that player
			if (serverBump == BumpType.Push || serverBump == BumpType.Blocked)
			{
				var worldTarget = state.WorldPosition.RoundToInt() + (Vector3Int) action.Direction();
				var swapee = MatrixManager.GetAt<PlayerSync>(worldTarget, true);
				if (swapee != null && swapee.Count > 0)
				{
					swapee[0].RollbackPosition();
				}
			}
		}
		if ( isClientBump || (serverBump != BumpType.None && serverBump != BumpType.Swappable)) {
			// we bumped something, an interaction might occur
			// try pushing things / opening doors
			if ( Validations.CanInteract(playerScript, NetworkSide.Server, allowCuffed: true) || serverBump == BumpType.ClosedDoor )
			{
				BumpInteract( state.WorldPosition, (Vector2) action.Direction() );
			}

			//don't change facing when diagonally opening a door
			var dir = action.Direction();
			if (!(dir.x != 0 && dir.y != 0 && serverBump == BumpType.ClosedDoor))
			{
				playerDirectional.FaceDirection( Orientation.From( action.Direction() ) );
			}

			return state;
		}

		//check for a swap
		bool swapped = false;
		if (serverBump == BumpType.Swappable)
		{
			swapped = CheckAndDoSwap(state.WorldPosition.RoundToInt() + action.Direction().To3Int(), action.Direction() * -1
				, isServer: true);
		}

		if ( IsNonStickyServer && !swapped ) {
			PushPull pushable;
			if (!swapped && IsAroundPushables( serverState, isServer: true, out pushable ) ) {
				StartCoroutine( InteractSpacePushable( pushable, action.Direction() ) );
			}
			return state;
		}

		if ( action.isNonPredictive )
		{
			Logger.Log( "Ignored action marked as Non-predictive while being indoors", Category.Movement );
			return state;
		}

		PlayerState nextState = NextState(state, action, true);

		nextState.Speed = SpeedServer;
		if (!playerScript.IsGhost)
		{
			playerScript.OnTileReached().Invoke(nextState.WorldPosition.RoundToInt());
			SoundManager.FootstepAtPosition(nextState.WorldPosition, playerScript.mind.stepType, gameObject);
		}

		return nextState;
	}

	#region walk interactions

	///Revert client push prediction straight ahead if it's wrong
	[Command]
	private void CmdValidatePush( GameObject pushable ) {
		var pushPull = pushable.GetComponent<PushPull>();
		if ( Validations.CanInteract(playerScript, NetworkSide.Server) || pushPull && !playerScript.IsInReach(pushPull.registerTile, true) ) {
			questionablePushables.Add( pushPull );
			Logger.LogWarningFormat( "Added questionable {0}", Category.PushPull, pushPull );
		}
	}

	/// <summary>
	/// Attempts to push things or open doors
	/// </summary>
	/// <param name="currentPosition">current world position</param>
	/// <param name="direction">direction of movement</param>
	private void BumpInteract(Vector3 currentPosition, Vector3 direction) {
			StartCoroutine( TryInteract( currentPosition, direction ) );
	}

	/// <summary>
	/// Tries to interact in a direciton, trying to open a closed door and push something.
	/// </summary>
	/// <param name="currentPosition">current world position</param>
	/// <param name="direction">direction of movement</param>
	/// <returns></returns>
	private IEnumerator TryInteract( Vector3 currentPosition, Vector3 direction ) {
		var worldPos = Vector3Int.RoundToInt(currentPosition);
		var worldTarget = Vector3Int.RoundToInt(currentPosition + direction);

		InteractDoor(worldPos, worldTarget);

//		Logger.LogTraceFormat( "{0} Interacting {1}->{2}, server={3}", Category.Movement, Time.unscaledTime*1000, worldPos, worldTarget, isServer );
		InteractPushable(worldPos, direction );

		yield return WaitFor.Seconds(.1f);
	}

	private IEnumerator InteractSpacePushable( PushPull pushable, Vector2 direction, bool isRecursive = false, int i = 0 ) {
		//Return if pushable is solid and you're trying to walk through it
		Vector3Int pushablePosition = pushable.Pushable.ServerPosition;
		if ( pushable.IsSolidServer && pushablePosition == this.ServerPosition + direction.RoundToInt() ) {
			Logger.LogTraceFormat( "Not doing anything: trying to push solid {0} through yourself", Category.PushPull, pushable.gameObject );
			yield break;
		}

		Logger.LogTraceFormat( (isRecursive ? "Recursive " : "") + "Trying to space push {0}", Category.PushPull, pushable.gameObject );

		if ( !isRecursive )
		{
			i = CalculateRequiredPushes( this.ServerPosition, pushablePosition, direction );
			Logger.LogTraceFormat( "Calculated {0} required pushes", Category.PushPull, i );
		}

		if ( i <= 0 ) yield break;

		Vector2 counterDirection = Vector2.zero - direction;

		pushable.QueuePush( Vector2Int.RoundToInt( counterDirection ) );
		i--;
		Logger.LogTraceFormat( "Queued obstacle push. {0} pushes left", Category.PushPull, i );

		if ( i <= 0 ) yield break;

		pushPull.QueuePush( Vector2Int.RoundToInt( direction ) );
		i--;
		Logger.LogTraceFormat( "Queued player push. {0} pushes left", Category.PushPull, i );


		if ( i > 0 )
		{
			StartCoroutine( InteractSpacePushable( pushable, direction, true, i ) );
		}

		yield return null;
	}

	private int CalculateRequiredPushes( Vector3 playerPos, Vector3Int pushablePos, Vector2 impulse ) {
		return 6;
	}
	/// <summary>tries to push a pushable</summary>
	/// <param name="worldOrigin">Tile you're interacting from</param>
	/// <param name="direction">Direction you're pushing</param>
	private void InteractPushable( Vector3Int worldOrigin, Vector3 direction ) {
		if ( IsNonStickyServer ) {
			return;
		}
		List<PushPull> pushables = MatrixManager.GetPushableAt(worldOrigin, direction.To2Int(), gameObject, isServer: true);
		if (pushables.Count > 0)
		{
			pushables[0].TryPush(direction.To2Int());
		}
	}

	/// <summary>
	/// Interact with a door at the specified position if there is a closed door there.
	/// </summary>
	/// <param name="currentPos">current world position </param>
	/// <param name="targetPos">position to interact with</param>
	private void InteractDoor(Vector3Int currentPos, Vector3Int targetPos)
	{
		// Make sure there is a door which can be interacted with
		InteractableDoor door = MatrixManager.GetClosedDoorAt(currentPos, targetPos, true);

		// Attempt to open door
		if (door != null)
		{
			door.Bump(gameObject);
		}
	}

		#endregion

	/// Lerping and ensuring server authority for space walk
	[Server]
	private void CheckMovementServer()
	{
		if ( !serverState.Active )
		{
			return;
		}
		//Space walk checks
		if ( IsNonStickyServer )
		{
			if (serverState.WorldImpulse == Vector2.zero && lastDirectionServer != Vector2.zero)
			{ //fixme: serverLastDirection is unreliable. maybe rethink notion of impulse
				//server initiated space dive.
				serverState.WorldImpulse = lastDirectionServer;
				serverState.ImportantFlightUpdate = true;
				serverState.ResetClientQueue = true;
			}

			//Perpetual floating sim
			if (ServerPositionsMatch)
			{
				if ( serverState.ImportantFlightUpdate )
				{
					NotifyPlayers();
				}
				else if ( consideredFloatingServer )
				{
					if ( floatingSyncHandle == null )
					{
						this.StartCoroutine( FloatingAwarenessSync(), ref floatingSyncHandle );
					}

					var oldPos = serverState.WorldPosition;

					//Extending prediction by one tile if player's transform reaches previously set goal
					//note: since this is a local position, the impulse needs to be converted to a local rotation,
					//hence the multiplication
					Vector3Int newGoal = Vector3Int.RoundToInt(serverState.Position + (Vector3)serverState.LocalImpulse(this));
					Vector3Int intOrigin = Vector3Int.RoundToInt(registerPlayer.WorldPosition + (Vector3)serverState.LocalImpulse(this));

					if (intOrigin.x > 18000 || intOrigin.x < -18000 || intOrigin.y > 18000 || intOrigin.y < -18000)
					{
						Stop();
						Logger.Log($"Player {transform.name} was forced to stop at {intOrigin}", Category.Movement);
						return;
					}
					serverState.Position = newGoal;
					ClearQueueServer();

					var newPos = serverState.WorldPosition;

					OnStartMove().Invoke( oldPos.RoundToInt(), newPos.RoundToInt() );
				}

				//Explicitly informing about stunned players
				//because they don't always meet clientside flight prediction expectations
				if ( registerPlayer.IsSlippingServer )
				{
					serverState.ImportantFlightUpdate = true;
					NotifyPlayers();
				}
			}
		}

		if ( consideredFloatingServer && !IsWeightlessServer ) {
			var worldOrigin = ServerPosition;
			var worldTarget = worldOrigin + serverState.WorldImpulse.RoundToInt();
			if ( registerPlayer.IsSlippingServer && MatrixManager.IsPassableAt( worldOrigin, worldTarget, true ) )
			{
				Logger.LogFormat( "Letting stunned {0} fly onto {1}", Category.Movement, gameObject.name, worldTarget );
				return;
			}
			if ( serverState.Speed >= PushPull.HIGH_SPEED_COLLISION_THRESHOLD && IsTileSnap )
			{
				//Stop first (reach tile), then inform about collision
				var collisionInfo = new CollisionInfo
				{
					Speed = serverState.Speed,
					Size = this.Size,
					CollisionTile = worldTarget
				};

				Stop();

				OnHighSpeedCollision().Invoke( collisionInfo );
			}
			else
			{
				Stop();
			}
		}
	}

	/// <summary>
	/// Send current position of space floating player to clients every second in case their reproduction is wrong
	/// </summary>
	private IEnumerator FloatingAwarenessSync()
	{
		yield return WaitFor.Seconds(1);
//		Logger.LogFormat( "{0} is floating at {1} (friendly reminder)", Category.Movement, gameObject.name, ServerPosition );
		serverState.ImportantFlightUpdate = true;
		NotifyPlayers();
		this.RestartCoroutine( FloatingAwarenessSync(), ref floatingSyncHandle );
	}

	public void Stop()
	{
		this.TryStopCoroutine( ref floatingSyncHandle );
		if ( consideredFloatingServer ) {
			PushPull spaceObjToGrab;
			if ( IsAroundPushables( serverState, isServer: true, out spaceObjToGrab ) && spaceObjToGrab.IsSolidServer ) {
				//some hacks to avoid space closets stopping out of player's reach
				var cnt = spaceObjToGrab.GetComponent<CustomNetTransform>();
				if ( cnt && cnt.IsFloatingServer && Vector2Int.RoundToInt(cnt.ServerState.WorldImpulse) == Vector2Int.RoundToInt(serverState.WorldImpulse) )
				{
					Logger.LogTraceFormat( "Caught {0} at {1} (registered at {2})", Category.Movement, spaceObjToGrab.gameObject.name,
							( Vector2 ) cnt.ServerState.WorldPosition, ( Vector2 ) ( Vector3 ) spaceObjToGrab.registerTile.WorldPositionServer );
					cnt.SetPosition( spaceObjToGrab.registerTile.WorldPositionServer );
					spaceObjToGrab.Stop();
				}
			}
			//removing lastDirection when we hit an obstacle in space
			lastDirectionServer = Vector2.zero;

			//finish floating. players will be notified as soon as serverState catches up
			serverState.WorldImpulse = Vector2.zero;
			serverState.ResetClientQueue = true;

			//Stopping spacewalk increases move number
			serverState.MoveNumber++;

			//Notify if position stayed the same
			NotifyPlayers();
		}
	}

	private void ServerLerp() {
		//Lerp on server if it's worth lerping
		//and inform players if serverState reached targetState afterwards
		Vector3 targetPos = serverState.WorldPosition;
		serverLerpState.WorldPosition =
			Vector3.MoveTowards( serverLerpState.WorldPosition, targetPos,
								 serverState.Speed * Time.deltaTime * serverLerpState.WorldPosition.SpeedTo(targetPos) );
		//failsafe
		var distance = Vector3.Distance( serverLerpState.WorldPosition, targetPos );
		if ( distance > 1.5 ) {
			Logger.LogWarning( $"Dist {distance} > 1:{serverLerpState}\n" +
			                   $"Target    :{serverState}", Category.Movement );
			serverLerpState.WorldPosition = targetPos;
		}
		if ( serverLerpState.WorldPosition == targetPos) {
			OnTileReached().Invoke( targetPos.RoundToInt() );
			// Check for swap once movement is done, to prevent us and another player moving into the same tile
			if (!playerScript.IsGhost)
			{
				CheckAndDoSwap(targetPos.RoundToInt(), lastDirectionServer * -1, isServer: true);
			}
		}
		if ( TryNotifyPlayers() ) {
			TryUpdateServerTarget();
		}
	}

	private void Cross(Vector3Int position)
	{
		if (PlayerUtils.IsGhost(gameObject))
		{
			return;
		}
		
		CheckTileSlip();

		var shoeSlot = playerScript.ItemStorage.GetNamedItemSlot( NamedSlot.feet );

		bool slipProtection = !shoeSlot.IsEmpty && shoeSlot.ItemAttributes.HasTrait( CommonTraits.Instance.NoSlip );

		if (slipProtection) return;
			var crossedItems = MatrixManager.GetAt<ItemAttributesV2>(position, true);
            foreach ( var crossedItem in crossedItems )
            {
                if ( crossedItem.HasTrait( CommonTraits.Instance.Slippery ) )
                {
                    registerPlayer.ServerSlip( slipWhileWalking: true );
                }
            }
        }

	public void CheckTileSlip()
	{

		var matrix = MatrixManager.Get(serverState.MatrixId);

		var shoeSlot = playerScript.ItemStorage.GetNamedItemSlot( NamedSlot.feet );

		bool slipProtection = !shoeSlot.IsEmpty && shoeSlot.ItemAttributes.HasTrait( CommonTraits.Instance.NoSlip );

		if (matrix.MetaDataLayer.IsSlipperyAt(ServerLocalPosition) && !slipProtection)
		{
			registerPlayer.ServerSlip();
		}
	}
}

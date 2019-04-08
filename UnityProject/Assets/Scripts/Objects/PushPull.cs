﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using Random = UnityEngine.Random;

public class PushPull : VisibleBehaviour {
	public const float DEFAULT_PUSH_SPEED = 6;
	[SyncVar]
	public bool isNotPushable = false;

	private IPushable pushableTransform;

	public IPushable Pushable {
		get {
			IPushable pushable;
			if ( pushableTransform != null ) {
				pushable = pushableTransform;
			} else {
				pushable = pushableTransform = GetComponent<IPushable>();
				pushable?.OnUpdateRecieved().AddListener( OnUpdateReceived );
				pushable?.OnTileReached().AddListener( OnServerTileReached );
				pushable?.OnClientTileReached().AddListener( OnClientTileReached );
				pushable?.OnPullInterrupt().AddListener( () =>
				{
					StopFollowing();
					ReleaseControl();//maybe it won't be required for all situations
				} );
			}
			return pushable;
		}
	}

	/// Just in case
	private void OnDestroy() {
		Pushable?.OnPullInterrupt().RemoveAllListeners();
		Pushable?.OnStartMove().RemoveAllListeners();
		Pushable?.OnTileReached().RemoveAllListeners();
		Pushable?.OnUpdateRecieved().RemoveAllListeners();
		Pushable?.OnClientStartMove().RemoveAllListeners();
		Pushable?.OnClientTileReached().RemoveAllListeners();
	}

	public bool IsSolid => !registerTile.IsPassable();


	#region Pull Master


	/// <summary>
	/// If this is server, returns IsPullingSomethingServer, otherwise returns IsPullingSomethingClient. Avoids having
	/// to check each separately depending on whether we are server or client.
	/// </summary>
	public bool IsPullingSomething => isServer ? IsPullingSomethingServer : IsPullingSomethingClient;
	/// <summary>
	/// If this is server, returns PulledObjectServer, otherwise returns PulledObjectClient. Avoids having
	/// to check each separately depending on whether we are server or client.
	/// </summary>
	public PushPull PulledObject => isServer ? PulledObjectServer : PulledObjectClient;

	public bool IsPullingSomethingServer => PulledObjectServer != null;
	public PushPull PulledObjectServer { get; private set; }

	public bool IsPullingSomethingClient => PulledObjectClient != null;
	public PushPull PulledObjectClient { get; set; }

	/// Client requests to stop pulling any objects
	[Command]
	public void CmdStopPulling() {
		ReleaseControl();
	}

	private void ReleaseControl() {
		if ( !IsPullingSomethingServer ) {
			return;
		}

		Logger.LogTraceFormat( "{0} stopped controlling {1}", Category.PushPull, this.gameObject.name, PulledObjectServer.gameObject.name );
		PulledObjectServer.PulledBy = null;
		PulledObjectServer = null;

		UpdatePullingUI(this);
	}

	/// Client asks to toggle pulling of given object
	[Command]
	public void CmdPullObject(GameObject pullableObject) {
		PushPull pullable = pullableObject.GetComponent<PushPull>();
		if ( !pullable ) {
			return;
		}
		if ( IsPullingSomethingServer ) {
			var alreadyPulling = PulledObjectServer;
			ReleaseControl();

			//Kill ex-pullable's impulses if we stop pulling it ourselves
			//todo: make it accept puller's impulse on release if he's flying
			alreadyPulling.Stop();

			//Just stopping pulling of object if we ctrl+click it again
			if ( alreadyPulling == pullable ) {
				return;
			}
		}
		ConnectedPlayer clientWhoAsked = PlayerList.Instance.Get( gameObject );
		if ( clientWhoAsked.Script.canNotInteract() )
		{
			return;
		}

		if ( PlayerScript.IsInReach( pullable.registerTile, this.registerTile )
		     && !pullable.isNotPushable && pullable != this && !IsBeingPulled ) {

			if ( pullable.StartFollowing( this ) ) {
				SoundManager.PlayNetworkedAtPos( "Rustle0" + Random.Range(1, 4), pullable.transform.position );

				PulledObjectServer = pullable;

				//Kill its impulses if we grabbed it
				PulledObjectServer.Stop();

				// Update the UI
				UpdatePullingUI(this);
			}
		}
	}

	[Server]
	private void UpdatePullingUI(PushPull pull)
	{
		ConnectedPlayer player = PlayerList.Instance.Get(pull.gameObject);

		if (player != ConnectedPlayer.Invalid)
			TargetUpdatePullingUI(player.Connection, pull.IsPullingSomethingServer);
	}

	[TargetRpc]
	private void TargetUpdatePullingUI(NetworkConnection target, bool pulling)
	{
		UIManager.Action.UpdatePullingUI(pulling);
	}

	#endregion

	private Coroutine revertPullHandle;

	public Vector2 InheritedImpulse => IsBeingPulled ? PulledBy.InheritedImpulse : Pushable.ServerImpulse;

	private IEnumerator RevertPullTimer() {
		yield return YieldHelper.Second;
		yield return YieldHelper.Second;

		if ( IsBeingPulledClient && !Pushable.IsMovingClient
			 && Pushable.ClientPosition != Pushable.TrustedPosition
		   )
		{
			Logger.LogFormat( "{0}: Reverted pull position", Category.PushPull, gameObject.name );
			Pushable.RollbackPrediction();
		} else {
			Logger.LogTraceFormat( "{0}: No need to revert pull position", Category.PushPull, gameObject.name );
		}
	}

	protected override void Awake() {
		base.Awake();
		var pushable = Pushable; //don't remove this, it initializes Pushable listeners ^

		followAction = (oldPos, newPos) => {
			Vector3Int currentPos = Pushable.ServerPosition;
			if ( oldPos == newPos || oldPos == TransformState.HiddenPos || newPos == currentPos ) {
				return;
			}
			Vector2Int followDir =  oldPos.To2Int() - currentPos.To2Int();
			if ( followDir == Vector2Int.zero ) {
				return;
			}
			if ( !TryFollow( currentPos, followDir, GetHeadSpeedServer() ) ) {
				StopFollowing();
			} else {
				PulledBy.NotifyPlayers(); // doubles messages for puller, but pulling looks proper even in high ping. might mess something up tho
//				Logger.Log( $"{gameObject.name}: following {PulledBy.gameObject.name} " +
//							$"from {currentSlavePos} to {masterPos} : {followDir}", Category.PushPull );
			}
		};
		predictiveFollowAction = (oldPos, newPos) => {
			Vector3Int currentPos = Pushable.ClientPosition;
			if ( oldPos == newPos || oldPos == TransformState.HiddenPos || newPos == currentPos ) {
				return;
			}
			var masterPos = oldPos.To2Int();
			var currentSlavePos = currentPos.To2Int();
			var followDir =  masterPos - currentSlavePos;
			if ( followDir == Vector2Int.zero ) {
				return;
			}
			if ( !TryPredictiveFollow( currentPos, oldPos, GetHeadSpeedClient() ) ) {
				Logger.LogError( $"{gameObject.name}: oops, predictive following {PulledByClient.gameObject.name} failed", Category.PushPull );
			} else {
				Logger.LogTraceFormat(
					"{0}: predictive following {1} from {2} to {3} : {4}", Category.PushPull,
					gameObject.name, PulledByClient.gameObject.name, currentSlavePos, masterPos, followDir );

			}
		};
	}

	/// <summary>
	/// Recursive method to get client speed of the train head
	/// </summary>
	public float GetHeadSpeedClient()
	{
		if ( IsBeingPulledClient )
		{
			return PulledByClient.GetHeadSpeedClient();
		}
		return Pushable.SpeedClient;
	}
	/// <summary>
	/// Recursive method to get server speed of the train head
	/// </summary>
	public float GetHeadSpeedServer()
	{
		if ( IsBeingPulled )
		{
			return PulledBy.GetHeadSpeedServer();
		}
		return Pushable.SpeedServer;
	}

	#region Pull

	private UnityAction<Vector3Int,Vector3Int> followAction;
	private UnityAction<Vector3Int,Vector3Int> predictiveFollowAction;

	public bool IsBeingPulled => PulledBy != null;
	private PushPull pulledBy;
	public PushPull PulledBy {
		get { return pulledBy; }
		private set {
			if ( IsBeingPulled ) {
				pulledBy.Pushable?.OnStartMove().RemoveListener( followAction );
				//inform previous master that it's over </3
				UninformHead( pulledBy, this );
			}

			if ( value != null )
			{
				value.Pushable?.OnStartMove().AddListener( followAction );
			}

			pulledBy = value;
			InformPullMessage.Send( this, this, pulledBy ); //inform slave of new master – or lack thereof

			if ( IsBeingPulled ) {
				InformHead( pulledBy, this);
			}
		}
	}

	///inform new master puller about who's pulling who in the train
	public void InformHead( PushPull whoToInform, PushPull subject = null ) {
		if ( subject == null ) {
			subject = this;
		}
		InformPullMessage.Send( whoToInform, subject, subject.PulledBy );
		if ( IsPullingSomethingServer ) {
			PulledObjectServer.InformHead( whoToInform, PulledObjectServer );
		}
	}

	private void UninformHead( PushPull whoToInform, PushPull subject ) {
		InformPullMessage.Send( whoToInform, subject, null );
		if ( IsPullingSomethingServer ) {
			PulledObjectServer.UninformHead( whoToInform, PulledObjectServer );
		}
	}

	public bool IsBeingPulledClient => PulledByClient != null;
	private PushPull pulledByClient;
	public PushPull PulledByClient {
		get { return pulledByClient; }
		set {
			if ( IsBeingPulledClient /*&& !isServer*/ ) { //toggle prediction here <v
				pulledByClient.Pushable?.OnClientStartMove().RemoveListener( predictiveFollowAction );
			}

			if ( value != null /*&& !isServer*/ )
			{
				value.Pushable?.OnClientStartMove().AddListener( predictiveFollowAction );
			}

			pulledByClient = value;
		}
	}

	/// (Eventually)
	public bool IsPulledByClient( PushPull pushPull ) {
		if ( !IsBeingPulledClient ) {
			return false;
		}

		return PulledByClient == pushPull || PulledByClient.IsPulledByClient( pushPull );
	}


	[Server]
	public bool StartFollowing( PushPull attachTo ) {
		if ( attachTo == this ) {
			return false;
		}
		//if attached to someone else:
		if ( IsBeingPulled ) {
			StopFollowing();
		}

		bool chooChooTrain = attachTo.IsBeingPulled && attachTo.PulledBy != this;

		//if puller can reach this + not trying to pull himself + not being pulled
		if ( PlayerScript.IsInReach( attachTo.registerTile, this.registerTile )
		     && attachTo != this && (!attachTo.IsBeingPulled || chooChooTrain) )
		{
			Logger.LogTraceFormat( "{0} started following {1}", Category.PushPull, this.gameObject.name, attachTo.gameObject.name );
			PulledBy = attachTo;
			return true;
		}

		return false;
	}
	/// Client requests to to break free
	[Command]
	public void CmdStopFollowing() {
		if ( !IsBeingPulled ) {
			return;
		}
		var player = PlayerList.Instance.Get( this.gameObject );
		if ( player != ConnectedPlayer.Invalid && !player.Script.canNotInteract() ) {
			StopFollowing();
		}
	}
	[Server]
	public void StopFollowing() {
		if ( !IsBeingPulled ) {
			return;
		}
		Logger.LogTraceFormat( "{0} stopped following {1}", Category.PushPull, this.gameObject.name, PulledBy.gameObject.name );

		PulledBy.PulledObjectServer = null;

		UpdatePullingUI(PulledBy);

		PulledBy = null;

		NotifyPlayers();
	}

	public virtual void OnCtrlClick()
	{
		TryPullThis();
	}

	[ContextMethod("Pull","Drag_Hand")]
	public void TryPullThis() {
		var initiator = PlayerManager.LocalPlayerScript.pushPull;
		//client pre-validation
		if ( PlayerScript.IsInReach( this.registerTile, initiator.registerTile ) && initiator != this ) {
			//client request: start/stop pulling
			initiator.CmdPullObject( gameObject );

			if ( PulledByClient == initiator ) {
				Logger.LogTraceFormat( "{0}: Breaking pull predictively", Category.PushPull, initiator.gameObject.name );
				PulledByClient.PulledObjectClient = null;
				PulledByClient = null;
			}
		}
	}
	[Server]
	private bool TryFollow( Vector3Int from, Vector2Int dir, float speed = Single.NaN ) {
		if ( !IsBeingPulled || isNotPushable || isPushing || Pushable == null )
		{
			return false;
		}

		if ( Mathf.Abs(dir.x) > 1 || Mathf.Abs(dir.y) > 1 ) {
			Logger.LogTrace( "oops="+dir, Category.PushPull );
			return false;
		}

		Vector3Int target = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
		if ( !MatrixManager.IsPassableAt( from, target, includingPlayers: false) ) //non-solid things can be pushed to player tile
		{
			return false;
		}

		bool success = Pushable.Push( dir, speed, true );
		if ( success ) {
			pushTarget = target;
//			Logger.LogTraceFormat( "Following {0}->{1}", Category.PushPull, from, target );
		}

		return success;
	}
	private bool TryPredictiveFollow( Vector3Int from, Vector3Int target, float speed = Single.NaN ) {
		if ( !IsBeingPulledClient || isNotPushable || Pushable == null )
		{
			return false;
		}

//		Vector3Int target = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
//		if ( !MatrixManager.IsPassableAt( from, target, false )
//		     ) {
//			return false;
//		}

		bool success = Pushable.PredictivePush( target.To2Int(), speed, true );
		if ( success ) {
//			Logger.LogTraceFormat( "Started predictive follow {0}->{1}", Category.PushPull, from, target );
		}

		return success;
	}
	#endregion

	#region Push fields

	//Server fields
	private bool isPushing;
	private Vector3Int pushTarget = TransformState.HiddenPos;
	private Queue<Tuple<Vector2Int, float>> pushRequestQueue = new Queue<Tuple<Vector2Int, float>>();

	//Client fields
	private PushState pushPrediction = PushState.None;
	private ApprovalState pushApproval = ApprovalState.None;
	private bool CanPredictPush => pushPrediction == PushState.None && Pushable.CanPredictPush;
	private Vector3Int predictivePushTarget = TransformState.HiddenPos;
	private Vector3Int lastReliablePos = TransformState.HiddenPos;

	#endregion

	#region Push

	[Server]
	public void QueuePush( Vector2Int dir, float speed = Single.NaN, bool allowDiagonals = false )
	{
		Logger.LogTraceFormat( "{0}: queued push {1} {2}", Category.PushPull, gameObject.name, dir, speed );
		pushRequestQueue.Enqueue( new Tuple<Vector2Int, float>(dir, speed) );
		CheckQueue();
	}

	private void CheckQueue()
	{
		if ( pushRequestQueue.Count > 0 && !isPushing )
		{
			var tuple = pushRequestQueue.Dequeue();
			if ( !TryPush(tuple.Item1, tuple.Item2 ) ) {
				pushRequestQueue.Clear();
			}
		}
	}


	[Server]
	public bool TryPush( Vector2Int dir, float speed = Single.NaN )
	{
		return TryPush( registerTile.WorldPosition, dir, speed );
	}

	/// <summary>
	/// Return true if a push from the specified position in the specified direction would
	/// cause this object to move. Does not actually move the object.
	/// </summary>
	/// <param name="from">position from which push is performed</param>
	/// <param name="dir">direction of the push</param>
	/// <param name="speed">speed of the push</param>
	/// <returns>true iff a push from the specified position in the specified direction would
	/// cause the object to move.</returns>
	public bool CanPush(Vector3Int from, Vector2Int dir, float speed = Single.NaN)
	{
		if (isNotPushable || isPushing || Pushable == null || !isAllowedDir(dir))
		{
			return false;
		}
		Vector3Int currentPos = registerTile.WorldPosition;
		if (from != currentPos)
		{
			return false;
		}

		if (Mathf.Abs(dir.x) > 1 || Mathf.Abs(dir.y) > 1)
		{
			Logger.LogTrace("oops=" + dir, Category.PushPull);
			return false;
		}

		Vector3Int target = from + Vector3Int.RoundToInt((Vector2)dir);
		if (!MatrixManager.IsPassableAt(from, target, includingPlayers: IsSolid)) //non-solid things can be pushed to player tile
		{
			return false;
		}

		return true;
	}

	[Server]
	public bool TryPush( Vector3Int from, Vector2Int dir, float speed = Single.NaN ) {
		if (!CanPush(from, dir, speed))
		{
			return false;
		}


		bool success = Pushable.Push( dir, speed );
		Vector3Int target = from + dir.To3Int();
		if ( success )
		{
			if ( IsBeingPulled && //Break pull only if pushable will end up far enough
				( pushRequestQueue.Count > 0 || !PlayerScript.IsInReach(PulledBy.registerTile.WorldPosition, target) ) )
			{
				StopFollowing();
			}
			if ( IsPullingSomethingServer && //Break pull only if pushable will end up far enough
			     ( pushRequestQueue.Count > 0 || !PlayerScript.IsInReach(PulledObjectServer.registerTile.WorldPosition, target) ) )
			{
				ReleaseControl();
			}
			isPushing = true;
			pushTarget = target;
			Logger.LogTraceFormat( "Started push {0}->{1}", Category.PushPull, from, target );
		}

		return success;
	}

	public bool TryPredictivePush( Vector3Int from, Vector2Int dir, float speed = Single.NaN ) {
		if ( isNotPushable || !CanPredictPush || Pushable == null || !isAllowedDir( dir ) ) {
			return false;
		}
		lastReliablePos = registerTile.WorldPosition;
		if ( from != lastReliablePos ) {
			return false;
		}
		Vector3Int target = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
		if ( !MatrixManager.IsPassableAt( from, target ) ||
		     MatrixManager.IsNoGravityAt( target ) ) { //not allowing predictive push into space
			return false;
		}

		bool success = Pushable.PredictivePush( target.To2Int(), speed );
		if ( success ) {
			pushPrediction = PushState.InProgress;
			pushApproval = ApprovalState.None;
			predictivePushTarget = target;
			Logger.LogTraceFormat( "Started predictive push {0}->{1}", Category.PushPull, from, target );
		}

		return success;
	}

	private void FinishPrediction()
	{
		Logger.LogTraceFormat( "Finishing predictive push", Category.PushPull );
		pushPrediction = PushState.None;
		pushApproval = ApprovalState.None;
		predictivePushTarget = TransformState.HiddenPos;
		lastReliablePos = TransformState.HiddenPos;
	}

	[Server]
	public void NotifyPlayers() {
		Pushable.NotifyPlayers();
	}

	private bool isAllowedDir( Vector2Int dir ) {
		return dir == Vector2Int.up || dir == Vector2Int.down || dir == Vector2Int.left || dir == Vector2Int.right
			|| dir == MINUS_ONE || dir == BOTTOM_RIGHT || dir == TOP_LEFT || dir == Vector2Int.one; //temp diagonal
	}

	enum PushState { None, InProgress, Finished }
	enum ApprovalState { None, Approved, Unexpected }

	#endregion

	#region Events

	private void OnServerTileReached( Vector3Int newPos ) {
		if ( !isPushing && pushRequestQueue.Count == 0 ) {
			return;
		}
//		Logger.LogTraceFormat( "{0}: {1} is reached ON SERVER", Category.PushPull, gameObject.name, pos );
		isPushing = false;
		if ( pushTarget != TransformState.HiddenPos &&
		     pushTarget != newPos &&
		     !MatrixManager.IsFloatingAt(gameObject, newPos))
		{
			//unexpected pos reported by server tile (common in space, space )
			pushRequestQueue.Clear();
		}
		CheckQueue();
	}

	/// For prediction
	private void OnUpdateReceived( Vector3Int serverPos ) {
		if ( IsBeingPulledClient ) {
			this.RestartCoroutine( RevertPullTimer(), ref revertPullHandle );
		}

		if ( pushPrediction == PushState.None ) {
			return;
		}

		pushApproval = serverPos == predictivePushTarget ? ApprovalState.Approved : ApprovalState.Unexpected;
		Logger.LogTraceFormat( "{0} predictive push to {1}", Category.PushPull, pushApproval, serverPos );

		//if predictive lerp is finished
		if ( pushApproval == ApprovalState.Approved ) {
			if ( pushPrediction == PushState.Finished ) {
				FinishPrediction();
			} else if ( pushPrediction == PushState.InProgress ) {
				Logger.LogTraceFormat( "Approved and waiting till lerp is finished", Category.PushPull );
			}
		} else if ( pushApproval == ApprovalState.Unexpected ) {
			var info = "";
			if ( serverPos == lastReliablePos ) {
				info += $"lastReliablePos match!({lastReliablePos})";
			} else {
				info += "NO reliablePos match";
			}
			if ( pushPrediction == PushState.Finished ) {
				info += ". Finishing!";
				FinishPrediction();
			} else {
				info += ". NOT Finishing yet";
			}
			Logger.LogFormat( "Unexpected push detected OnUpdateRecieved {0}", Category.PushPull, info );
		}
	}

	/// For prediction
	private void OnClientTileReached( Vector3Int pos ) {
		if ( pushPrediction == PushState.None ) {
			return;
		}

		if ( pos != predictivePushTarget ) {
			Logger.LogFormat( "Lerped to {0} while target pos was {1}", Category.PushPull, pos, predictivePushTarget );
			if ( MatrixManager.IsNoGravityAt( pos ) ) {
				Logger.LogTraceFormat( "...uh, we assume it's a space push and finish prediction", Category.PushPull );
				FinishPrediction();
			}
			return;
		}

		Logger.LogTraceFormat( "{0} is reached ON CLIENT, approval={1}", Category.PushPull, pos, pushApproval );
		pushPrediction = PushState.Finished;
		switch ( pushApproval ) {
			case ApprovalState.Approved:
				//ok, finishing
				FinishPrediction();
				break;
			case ApprovalState.Unexpected:
				Logger.LogFormat( "Invalid push detected in OnClientTileReached, finishing", Category.PushPull );
				FinishPrediction();
				break;
			case ApprovalState.None:
				Logger.LogTraceFormat( "Finished lerp, waiting for server approval...", Category.PushPull );
				break;
		}
	}

	#endregion

	//Stop object
	public void Stop() {
		Pushable.Stop();
	}

	public static readonly Vector2Int MINUS_ONE = new Vector2Int(-1,-1);
	public static readonly Vector2Int TOP_LEFT = new Vector2Int(-1,1);
	public static readonly Vector2Int BOTTOM_RIGHT = new Vector2Int(1,-1);
#if UNITY_EDITOR
	private static Color color1 = Color.red;
	private static Color color2 = Color.cyan;
	private static Vector3 offset = new Vector2(0.03f,0.05f);

	private void OnDrawGizmos() {
		if ( IsBeingPulled ) {
			Gizmos.color = color1;
			GizmoUtils.DrawArrow( transform.position, PulledBy.transform.position - transform.position, 0.1f );
		}
		if ( IsBeingPulledClient ) {
			Gizmos.color = color2;
			Vector3 offPosition = transform.position + offset;
			GizmoUtils.DrawArrow( offPosition, (PulledByClient.transform.position + offset) - offPosition, 0.1f );
		}
	}
#endif
}
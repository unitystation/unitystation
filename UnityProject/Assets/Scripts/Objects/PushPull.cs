using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour {
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
				pushable?.OnPullInterrupt().AddListener( () => StopFollowing() );
			}
			return pushable;
		}
	}

	public bool IsSolid => !registerTile.IsPassable();

	protected override void Awake() {
		base.Awake();
		var pushable = Pushable;
	}

	#region Push fields

	//Server fields
	private bool isPushing;
	private Vector3Int pushTarget = TransformState.HiddenPos;
	private Queue<Vector2Int> pushRequestQueue = new Queue<Vector2Int>();

	//Client fields
	private PushState prediction = PushState.None;
	private ApprovalState approval = ApprovalState.None;
	private bool CanPredictPush => prediction == PushState.None && Pushable.CanPredictPush;
	private Vector3Int predictivePushTarget = TransformState.HiddenPos;
	private Vector3Int lastReliablePos = TransformState.HiddenPos;

	#endregion

	#region Pull Master

	public bool IsPullingSomething => ControlledObject != null;
	public PushPull ControlledObject;

	/// Client requests to stop pulling any objects
	[Command]
	public void CmdStopPulling() {
		ReleaseControl();
	}

	private void ReleaseControl() {
		if ( IsPullingSomething ) {
			ControlledObject.AttachedTo = null;
		}
		Logger.LogTraceFormat( "{0} stopped controlling {1}", Category.PushPull, this.gameObject.name, ControlledObject?.gameObject.name );
		ControlledObject = null;
	}

	/// Client asks to start pulling given object
	[Command]
	public void CmdPullObject(GameObject pullable) {
		var slaveObject = pullable.GetComponent<PushPull>();
		if ( !slaveObject ) {
			return;
		}
		if ( slaveObject.StartFollowing(this) ) {
			ControlledObject = slaveObject;
		}
	}

	#endregion

	#region Pull

	public bool IsBeingPulled => AttachedTo != null;
	public PushPull AttachedTo;

	[Server]
	public bool StartFollowing( PushPull attachTo ) {
		if ( attachTo == this ) {
			return false;
		}
		//later: experiment with allowing pulling while being pulled, but add condition against deadlocks
		//if puller can reach this + not trying to pull himself + not being pulled
		if ( PlayerScript.IsInReach( attachTo.registerTile.WorldPosition, this.registerTile.WorldPosition )
		     && attachTo != this && !attachTo.IsBeingPulled )
		{
			Logger.LogTraceFormat( "{0} started following {1}", Category.PushPull, this.gameObject.name, attachTo.gameObject.name );
			AttachedTo = attachTo;
			return true;
		}

		return false;
	}
	[Server]
	public void StopFollowing() {
		if ( !IsBeingPulled ) {
			return;
		}
		AttachedTo.ControlledObject = null;
		AttachedTo = null;
		Logger.LogTraceFormat( "{0} stopped following {1}", Category.PushPull, this.gameObject.name, AttachedTo?.gameObject.name );
	}

	public virtual void OnCtrlClick()
	{
		TryPullThis();
	}

	public void TryPullThis() {
		var initiator = PlayerManager.LocalPlayerScript.pushPull;
		//client pre-validation
		if ( PlayerScript.IsInReach( initiator.registerTile.WorldPosition, this.registerTile.WorldPosition )
		     && initiator != this && !initiator.IsBeingPulled ) {
			PushPull pullingObject = initiator.ControlledObject;
			if ( pullingObject ) {
				//client request: stop pulling
				initiator.CmdStopPulling();
				//Just stopping pulling of object if we ctrl+click it again
				if ( pullingObject == this ) {
					return;
				}
			}

			//client request: start pulling
			initiator.CmdPullObject( gameObject );

			//Predictive pull:
//				if (customNetTransform != null)
//				{
//					customNetTransform.enabled = false;
//				}
//				p.PlayerSync.PullingObject = gameObject;
		}
	}

//	public void CancelPullBehaviour()
//	{
//		if (pulledBy == PlayerManager.LocalPlayer) {
//			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
//			return;
//		}
//		if (pulledBy != PlayerManager.LocalPlayer) {
//			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopOtherPulling(gameObject);
//		}
//		PlayerManager.LocalPlayerScript.PlayerSync.PullReset(gameObject.GetComponent<NetworkIdentity>().netId);
//	}
//
//	[SyncVar] public GameObject pulledBy;
//
//	//SyncVar to make sure the state is synced with new players
//	[SyncVar] public bool custNetActiveState;
//
//	public override void OnStartServer(){
//		custNetActiveState = true;
//		base.OnStartServer();
//	}

//	[HideInInspector] public bool isPulling;
//	public void BreakPull()
//	{
//		PlayerScript player = PlayerManager.LocalPlayerScript;
//		GameObject pullingObject = player.PlayerSync.PullingObject;
//		if (!pullingObject || !pullingObject.Equals(gameObject)) {
//			return;
//		}
//		player.PlayerSync.PullReset(NetworkInstanceId.Invalid);
//		player.PlayerSync.PullingObject = null;
//		player.PlayerSync.PullObjectID = NetworkInstanceId.Invalid;
//		player.playerNetworkActions.isPulling = false;
//		pulledBy = null;
//	}
//
//	[Command]
//	public void CmdPullObject(GameObject obj)
//	{
//		if (isPulling)
//		{
//			GameObject cObj = gameObject.GetComponent<IPlayerSync>().PullingObject;
//			cObj.GetComponent<PushPull>().pulledBy = null;
//			gameObject.GetComponent<IPlayerSync>().PullObjectID = NetworkInstanceId.Invalid;
//		}
//
//		PushPull pulled = obj.GetComponent<PushPull>();
//		//Stop CNT as the transform of the pulled obj is now handled by PlayerSync
//		pulled.RpcToggleCnt(false);
//		//Cache value for new players
//		pulled.custNetActiveState = false;
//		//check if the object you want to pull is another player
//		if (pulled.isPlayer)
//		{
//			IPlayerSync playerS = obj.GetComponent<IPlayerSync>();
//			//Anything that the other player is pulling should be stopped
//			if (playerS.PullingObject != null)
//			{
//				PlayerNetworkActions otherPNA = obj.GetComponent<PlayerNetworkActions>();
//				otherPNA.CmdStopOtherPulling(playerS.PullingObject);
//			}
//		}
//		//Other player is pulling object, send stop on that player
//		if (pulled.pulledBy != null)
//		{
//			if (pulled.pulledBy != gameObject)
//			{
//				pulled.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
//			}
//		}
//
//		if (pulled != null)
//		{
//			IPlayerSync pS = GetComponent<IPlayerSync>();
//			pS.PullObjectID = pulled.netId;
//			isPulling = true;
//		}
//	}
//
//	//if two people try to pull the same object
//	[Command]
//	public void CmdStopOtherPulling(GameObject obj)
//	{
//		PushPull objA = obj.GetComponent<PushPull>();
//		objA.custNetActiveState = true;
//		if (objA.pulledBy != null)
//		{
//			objA.pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(obj);
//		}
//		var netTransform = obj.GetComponent<CustomNetTransform>();
//		if (netTransform != null) {
//			netTransform.SetPosition(obj.transform.localPosition);
//		}
//	}
//
//	[Command]
//	public void CmdStopPulling(GameObject obj)
//	{
//		if (!isPulling)
//		{
//			return;
//		}
//
//		isPulling = false;
//		PushPull pulled = obj.GetComponent<PushPull>();
//		pulled.RpcToggleCnt(true);
//		//Cache value for new players
//		pulled.custNetActiveState = true;
//		if (pulled != null)
//		{
//			//			//this triggers currentPos syncvar hook to make sure registertile is been completed on all clients
//			//			pulled.currentPos = pulled.transform.position;
//
//			IPlayerSync pS = gameObject.GetComponent<IPlayerSync>();
//			pS.PullObjectID = NetworkInstanceId.Invalid;
//			pulled.pulledBy = null;
//		}
//		var netTransform = obj.GetComponent<CustomNetTransform>();
//		if (netTransform != null) {
//			netTransform.SetPosition(obj.transform.localPosition);
//		}
//	}

	#endregion

	#region Push

	[Server]
	public void QueuePush( Vector2Int dir )
	{
		pushRequestQueue.Enqueue( dir );
		CheckQueue();
	}

	private void CheckQueue()
	{
		if ( pushRequestQueue.Count > 0 && !isPushing )
		{
			TryPush( pushRequestQueue.Dequeue() );
		}
	}


	[Server]
	public bool TryPush( Vector2Int dir )
	{
		return TryPush( registerTile.WorldPosition, dir );
	}

	[Server]
	public bool TryPush( Vector3Int from, Vector2Int dir ) {
		if ( isNotPushable || isPushing || Pushable == null || !isAllowedDir( dir ) ) {
			return false;
		}
		Vector3Int currentPos = registerTile.WorldPosition;
		if ( from != currentPos ) {
			return false;
		}
		Vector3Int target = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
		if ( !MatrixManager.IsPassableAt( from, target, IsSolid ) ) //non-solid things can be pushed to player tile
		{
			return false;
		}

		if ( IsBeingPulled ) {
			StopFollowing();
		}

		bool success = Pushable.Push( dir );
		if ( success ) {
			isPushing = true;
			pushTarget = target;
			Logger.LogTraceFormat( "Started push {0}->{1}", Category.PushPull, from, target );
		}

		return success;
	}

	public bool TryPredictivePush( Vector3Int from, Vector2Int dir ) {
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

		bool success = Pushable.PredictivePush( dir );
		if ( success ) {
			prediction = PushState.InProgress;
			approval = ApprovalState.None;
			predictivePushTarget = target;
			Logger.LogTraceFormat( "Started predictive push {0}->{1}", Category.PushPull, from, target );
		}

		return success;
	}

	private void FinishPrediction()
	{
		Logger.LogTraceFormat( "Finishing predictive push", Category.PushPull );
		prediction = PushState.None;
		approval = ApprovalState.None;
		predictivePushTarget = TransformState.HiddenPos;
		lastReliablePos = TransformState.HiddenPos;
	}

	[Server]
	public void NotifyPlayers() {
		Pushable.NotifyPlayers();
	}

	private bool isAllowedDir( Vector2Int dir ) {
		return dir == Vector2Int.up || dir == Vector2Int.down || dir == Vector2Int.left || dir == Vector2Int.right;
	}

	enum PushState { None, InProgress, Finished }
	enum ApprovalState { None, Approved, Unexpected }

	#endregion

	#region Events

	private void OnServerTileReached( Vector3Int pos ) {
//		Logger.LogTraceFormat( "{0}: {1} is reached ON SERVER", Category.PushPull, gameObject.name, pos );
		isPushing = false;
		if ( pushTarget != TransformState.HiddenPos &&
		     pushTarget != pos )
		{
			//unexpected pos reported by server tile (common in space, space )
			pushRequestQueue.Clear();
		}
		CheckQueue();
	}

	/// For prediction
	private void OnUpdateReceived( Vector3Int serverPos ) {
		if ( prediction == PushState.None ) {
			return;
		}

		approval = serverPos == predictivePushTarget ? ApprovalState.Approved : ApprovalState.Unexpected;
		Logger.LogTraceFormat( "{0} predictive push to {1}", Category.PushPull, approval, serverPos );

		//if predictive lerp is finished
		if ( approval == ApprovalState.Approved ) {
			if ( prediction == PushState.Finished ) {
				FinishPrediction();
			} else if ( prediction == PushState.InProgress ) {
				Logger.LogTraceFormat( "Approved and waiting till lerp is finished", Category.PushPull );
			}
		} else if ( approval == ApprovalState.Unexpected ) {
			var info = "";
			if ( serverPos == lastReliablePos ) {
				info += $"lastReliablePos match!({lastReliablePos})";
			} else {
				info += "NO reliablePos match";
			}
			if ( prediction == PushState.Finished ) {
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
		if ( prediction == PushState.None ) {
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

		Logger.LogTraceFormat( "{0} is reached ON CLIENT, approval={1}", Category.PushPull, pos, approval );
		prediction = PushState.Finished;
		switch ( approval ) {
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

#if UNITY_EDITOR
	private static Color color1 = Color.red;

	private void OnDrawGizmos() {
		if ( !IsBeingPulled ) {
			return;
		}
		Gizmos.color = color1;
//		GizmoUtils.DrawArrow( registerTile.WorldPosition, AttachedTo.registerTile.WorldPosition, 0.1f );
		GizmoUtils.DrawArrow( transform.position, AttachedTo.transform.position - transform.position, 0.1f );
	}
#endif
}
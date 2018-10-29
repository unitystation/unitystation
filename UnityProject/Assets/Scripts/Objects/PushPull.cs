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
			}
			return pushable;
		}
	}

	public bool IsSolid => !registerTile.IsPassable();
	public bool IsBeingPulled => false;

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

	#region Pull

	public virtual void OnMouseDown()
	{
		//if control clicking
		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)) {
//			var ps = PlayerManager.LocalPlayerScript;
//			if (ps.IsInReach(transform.position) && transform != ps.transform && !ps.pushPull.IsBeingPulled)
			//if local player can reach this + not trying to pull himself + not being pulled
			{
//				if (ps.PlayerSync.PullingObject != null && ps.PlayerSync.PullingObject != gameObject)
				//if pulling something that's not this object
				{
//					p.playerNetworkActions.CmdStopPulling(p.PlayerSync.PullingObject);
					//client request: stop pulling
				}
				//client request: start pulling
//				ps.playerNetworkActions.CmdPullObject(gameObject);
				//Predictive pull:
//				if (customNetTransform != null)
				{
//					customNetTransform.enabled = false;
				}
//				p.PlayerSync.PullingObject = gameObject;
			}
		} else {
//			//If this is an item with a pick up trigger and player is
//			//not holding control, then check if it is being pulled
//			//before adding to inventory
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

	[Server]
	public void StartPull() {
	}
	[Server]
	public void StopPull() {
	}
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
		//TODO: pull checks
//		if (pulledBy != null) {
//			if (pulledBy != PlayerManager.LocalPlayer) {
//				pulledBy.GetComponent<PlayerNetworkActions>().CmdStopOtherPulling(gameObject);
//			} else {
//				pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(gameObject);
//				PlayerManager.LocalPlayerScript.playerNetworkActions.isPulling = false;
//				PlayerManager.LocalPlayerScript.PlayerSync.PullingObject = null;
//			}
//
//			pulledBy = null;
//		}

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

	#region old
//
//	public virtual void OnMouseDown()
//	{
//		// PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null condition makes sure that the player itself
//		// isn't being pulled. If he is then he is not allowed to pull anything else as this can cause problems
//		if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.LeftCommand)){
//			if (PlayerManager.LocalPlayerScript.IsInReach(transform.position) &&
//				transform != PlayerManager.LocalPlayerScript.transform && PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null) {
//				if (PlayerManager.LocalPlayerScript.PlayerSync.PullingObject != null &&
//				   PlayerManager.LocalPlayerScript.PlayerSync.PullingObject != gameObject) {
//					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(PlayerManager.LocalPlayerScript.PlayerSync.PullingObject);
//				}
//
//				if (pulledBy == PlayerManager.LocalPlayer) {
//					CancelPullBehaviour();
//				} else {
//					CancelPullBehaviour();
//					PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPullObject(gameObject);
//					//Predictive pull:
//					if (customNetTransform != null) {
//						customNetTransform.enabled = false;
//					}
//					PlayerManager.LocalPlayerScript.PlayerSync.PullingObject = gameObject;
//				}
//			}
//		}
//	}
//
//	//This is to turn off CNT while pulling an object
//	[ClientRpc]
//	public void RpcToggleCnt(bool activeState){
//		if(customNetTransform != null){
//			customNetTransform.enabled = activeState;
//		}
//	}
//
//	private void LateUpdate()
//	{
//		if (CustomNetworkManager.Instance._isServer) {
//			if (transform.hasChanged) {
//				transform.hasChanged = false;
//				currentPos = transform.localPosition;
//			}
//		}
//	}

	#endregion

	#region PNA


	#endregion

	//Stop object
	public void Stop() {
		Pushable.Stop();
	}
}
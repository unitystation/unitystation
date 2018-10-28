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
//	[SyncVar] public GameObject pulledBy;
//
//	//SyncVar to make sure the state is synced with new players
//	[SyncVar] public bool custNetActiveState;
//
//	public override void OnStartServer(){
//		custNetActiveState = true;
//		base.OnStartServer();
//	}
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
//	public void TryPush(GameObject pushedBy, Vector2 pushDir)
//	{
//		if (pushDir != Vector2.up && pushDir != Vector2.right && pushDir != Vector2.down && pushDir != Vector2.left) {
//			return;
//		}
//		if (pushing || !isPushable || customNetTransform.isPushing
//			|| customNetTransform.predictivePushing) {
//			return;
//		}
//
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
//
//		}
//
//		Vector3Int newPos = Vector3Int.RoundToInt(transform.localPosition + (Vector3)pushDir);
//
//		if (matrix.IsPassableAt(newPos) || matrix.ContainsAt(newPos, gameObject)) {
//			//Start the push on the client, then start on the server, the server then tells all other clients to start the push also
//			pusher = pushedBy;
//			pushTarget = newPos;
//			//Start command to push on server
//			if (pusher == PlayerManager.LocalPlayer) {
//				//pushing for local player is set to true from CNT, to make sure prediction isn't overwhelmed
//				customNetTransform.PushToPosition(pushTarget, PlayerManager.LocalPlayerScript.playerMove.speed, this);
//				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, transform.localPosition, pushTarget,
//				                                                                PlayerManager.LocalPlayerScript.playerMove.speed);
//			}
//		}
//	}
//
//
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
//	//This is to turn off CNT while pulling an object
//	[ClientRpc]
//	public void RpcToggleCnt(bool activeState){
//		if(customNetTransform != null){
//			customNetTransform.enabled = activeState;
//		}
//	}
//
//
//	private void Update()
//	{
//		if (pushing) {
//			if (customNetTransform.predictivePushing) {
//				//Wait for the server to catch up to the pushtarget if predictivePushing is true
//				if (Vector3.Distance(currentPos, pushTarget) < 0.1f) {
//					//if it is then set it to false, this ensures that the player cannot keep pushing if
//					//he is experiencing high lag by waiting for the server position to match up
//					customNetTransform.predictivePushing = false;
//				}
//				return;
//			}
//
//			if (Vector3.Distance(transform.localPosition, pushTarget) < 0.1f && !customNetTransform.isPushing) {
//				pushing = false;
//				customNetTransform.predictivePushing = false;
//				registerTile.UpdatePosition();
//			}
//			if(pushing && customNetTransform.IsFloatingClient && customNetTransform.IsInSpace){
//				pushing = false;
//				customNetTransform.predictivePushing = false;
//			}
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

//	[HideInInspector] public bool isPulling;
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

	//Stop object
	public void Stop() {
		Pushable.Stop();
	}
}
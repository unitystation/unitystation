using System;
using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour {
	private IPushable pushableTransform;
	public IPushable Pushable {
		get {
			IPushable pushable;
			if ( pushableTransform != null ) {
				pushable = pushableTransform;
			} else {
				pushable = pushableTransform = GetComponent<IPushable>();
				pushable?.OnTileReached().AddListener( pos => {
					Logger.LogTraceFormat( "{0} is reached ON SERVER", Category.PushPull, pos );
					isPushing = false;
				} );
				pushable?.OnUpdateRecieved().AddListener( serverPos => {
					if ( prediction == PushState.None ){
						return;
					}
					approval = serverPos == predictivePushTarget ? ApprovalState.Approved : ApprovalState.Invalid;
					Logger.LogTraceFormat( "{0} predictive push to {1}", Category.PushPull, approval, serverPos );

					//if predictive lerp is finished
					if ( approval == ApprovalState.Approved )
					{
						if ( prediction == PushState.Finished )
						{
							FinishPush();
						}
						else if ( prediction == PushState.InProgress )
						{
							Logger.LogTraceFormat( "Approved and waiting till lerp is finished", Category.PushPull );
						}
					}
					else if ( approval == ApprovalState.Invalid )
					{
						Logger.LogWarningFormat( "Invalid push detected in OnUpdateRecieved!", Category.PushPull );
					}

				} );
				pushable?.OnClientTileReached().AddListener( pos => {
					if ( prediction == PushState.None ){
						return;
					}
					if ( pos != predictivePushTarget )
					{
						Logger.LogWarningFormat( "Lerped to {0} while target pos was {1}, wtf?", Category.PushPull, pos, predictivePushTarget );
						return;
					}
					Logger.LogTraceFormat( "{0} is reached ON CLIENT, approval={1}", Category.PushPull, pos, approval );
					prediction = PushState.Finished;
					switch ( approval )
					{
						/*|| approval == ApprovalState.Invalid*/
						case ApprovalState.Approved:
							//ok, finishing
							FinishPush();
							break;
						case ApprovalState.Invalid:
							Logger.LogWarningFormat( "Invalid push detected in OnClientTileReached!", Category.PushPull );
							break;
						case ApprovalState.None:
							Logger.LogTraceFormat( "Finished lerp, waiting for server approval...", Category.PushPull );
							break;
					}
				} );
			}
			return pushable;
		}
	}

	private void FinishPush()
	{
		Logger.LogTraceFormat( "Finishing predictive push", Category.PushPull );
		prediction = PushState.None;
		approval = ApprovalState.None;
		predictivePushTarget = TransformState.HiddenPos;
	}

	public bool CanBePushed => !registerTile.IsPassable();

	//Server fields
	private bool isPushing;
//	private Vector3Int pushTarget = TransformState.HiddenPos;

	//Client fields
	private PushState prediction = PushState.None;
	private ApprovalState approval = ApprovalState.None;
	private bool allowedToPush => prediction == PushState.None;
	private Vector3Int predictivePushTarget = TransformState.HiddenPos;

	[Server]
	public bool TryPush( Vector3Int from, Vector2Int dir ) {
		if ( !CanBePushed || isPushing || Pushable == null ) {
			return false;
		}
		Vector3Int currentPos = registerTile.WorldPosition;
		if ( from != currentPos ) {
			return false;
		}
		Vector3Int target = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
		if ( Vector2.Distance( (Vector3)from, (Vector3)currentPos) > 1 ||
		     !MatrixManager.IsPassableAt( target ) ) {
			return false;
		}

		Logger.LogTraceFormat( "Started push {0}->{1}", Category.PushPull, from, target );
		isPushing = true;
//		pushTarget = target;
		Pushable.Push( dir );

		return true;
	}

	public bool TryPredictivePush( Vector3Int from, Vector2Int dir ) {
		if ( !CanBePushed || !allowedToPush || Pushable == null ) {
			return false;
		}
		Vector3Int currentPos = registerTile.WorldPosition;
		if ( from != currentPos ) {
			return false;
		}
		Vector3Int target = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
		if ( Vector2.Distance( (Vector3)from, (Vector3)currentPos) > 1 ||
		     !MatrixManager.IsPassableAt( target ) ) {
			return false;
		}

		Logger.LogTraceFormat( "Started predictive push {0}->{1}", Category.PushPull, from, target );
		prediction = PushState.InProgress;
		approval = ApprovalState.None;
		predictivePushTarget = target;
		Pushable.PredictivePush( dir );

		return true;
	}

	enum PushState { None, InProgress, Finished }
	enum ApprovalState { None, Approved, Invalid }

	#region cnt
//	/// Client side prediction for pushing
//	/// This allows instant pushing reaction to a pushing event
//	/// on the client who instigated it. The server then validates
//	/// the transform position and returns it if it is illegal
//	public void PushToPosition( Vector3 pos, float speed, PushPull pushComponent ) {
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
//	}
	#endregion

	#region old

//	private Matrix matrix => registerTile.Matrix;
//	private CustomNetTransform customNetTransform;
//
//	[SyncVar] public GameObject pulledBy;
//
//	//SyncVar to make sure the state is synced with new players
//	[SyncVar] public bool custNetActiveState;
//
//	public bool pushing;
//
//	public Vector3 pushTarget;
//
//	public GameObject pusher { get; private set; }
//
//	public override void OnStartClient()
//	{
//		StartCoroutine(WaitForLoad());
//
//		base.OnStartClient();
//	}
//
//	public override void OnStartServer(){
//		custNetActiveState = true;
//		base.OnStartServer();
//	}
//
//	private IEnumerator WaitForLoad()
//	{
//		yield return new WaitForSeconds(2f);
//
//		if (registerTile == null) {
//			registerTile = GetComponent<RegisterTile>();
//		}
//
//		registerTile.UpdatePosition();
//
//		customNetTransform = GetComponent<CustomNetTransform>();
//		//Sync state with new players
//		if (customNetTransform != null) {
//			customNetTransform.enabled = custNetActiveState;
//		}
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
//
//	[Command]
//	public void CmdTryPush(GameObject obj, Vector3 startLocalPos, Vector3 targetPos, float speed)
//	{
//		PushPull pushed = obj.GetComponent<PushPull>();
//		if (pushed != null)
//		{
//			var netTransform = obj.GetComponent<CustomNetTransform>();
//			netTransform.PushTo(targetPos, playerSprites.currentDirection.Vector, true, speed, true);
//		}
//	}

	#endregion
}
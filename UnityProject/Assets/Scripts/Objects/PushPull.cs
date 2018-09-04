using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour {
//	public bool allowedToMove = true;
//
//	[SyncVar] public Vector3 currentPos;
	public bool isPushable = true;
	//push limits: U,D,L,R only
	private PlayerSync playerSync;
	public PlayerSync PlayerSync => playerSync ? playerSync : ( playerSync = GetComponent<PlayerSync>() );
	private CustomNetTransform netTransform;
	public CustomNetTransform NetTransform => netTransform ? netTransform : ( netTransform = GetComponent<CustomNetTransform>() );

	[Server]
	public bool TryPush( Vector3Int from, Vector2Int dir ) {
		Vector3Int currentPos = registerTile.WorldPosition;
		if ( from != currentPos ) {
			return false;
		}
		Vector3Int to = from + Vector3Int.RoundToInt( ( Vector2 ) dir );
		Logger.LogTraceFormat( "Attempting push {0}->{1}", Category.PushPull, from, to );
		if ( !isPushable ||
		     Vector2.Distance( (Vector3)from, (Vector3)currentPos) > 1 ||
		     !MatrixManager.IsPassableAt( to, to ) ) {
			return false;
		}
		if ( NetTransform ) {
			NetTransform.Push( dir );
		} else if ( PlayerSync ) {
			PlayerSync.Push( dir );
		} else {
			return false;
		}
		return true;
	}

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
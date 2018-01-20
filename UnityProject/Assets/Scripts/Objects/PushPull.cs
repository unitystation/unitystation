using System.Collections;
using PlayGroup;
using Tilemaps;
using Tilemaps.Behaviours.Objects;
using Tilemaps.Scripts;
using UnityEngine;
using UnityEngine.Networking;

public class PushPull : VisibleBehaviour
{
	public bool allowedToMove = true;

	[SyncVar] public Vector3 currentPos;
	public bool isPushable = true;

	private Matrix matrix => registerTile.Matrix;
	private CustomNetTransform customNetTransform;

	[SyncVar] public GameObject pulledBy;

	//SyncVar to make sure the state is synced with new players
	[SyncVar] public bool custNetActiveState;

	public bool pushing;

	public Vector3 pushTarget;

	public GameObject pusher { get; private set; }

	public override void OnStartClient()
	{
		StartCoroutine(WaitForLoad());

		base.OnStartClient();
	}

	public override void OnStartServer(){
		custNetActiveState = true;
		base.OnStartServer();
	}

	private IEnumerator WaitForLoad()
	{
		yield return new WaitForSeconds(2f);

		if (registerTile == null) {
			registerTile = GetComponent<RegisterTile>();
		}

		registerTile.UpdatePosition();

		customNetTransform = GetComponent<CustomNetTransform>();
		//Sync state with new players
		if (customNetTransform != null) {
			customNetTransform.enabled = custNetActiveState;
		}
	}

	public virtual void OnMouseDown()
	{
		// PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null condition makes sure that the player itself
		// isn't being pulled. If he is then he is not allowed to pull anything else as this can cause problems
		if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform.position) &&
			transform != PlayerManager.LocalPlayerScript.transform && PlayerManager.LocalPlayerScript.playerMove.pushPull.pulledBy == null) {
			if(PlayerManager.LocalPlayerScript.playerSync.pullingObject != null && 
			   PlayerManager.LocalPlayerScript.playerSync.pullingObject != gameObject){
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(PlayerManager.LocalPlayerScript.playerSync.pullingObject);
			}

			if (pulledBy == PlayerManager.LocalPlayer) {
				CancelPullBehaviour();
			} else {
				CancelPullBehaviour();
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPullObject(gameObject);
				//Predictive pull:
				if (customNetTransform != null) {
					customNetTransform.enabled = false;
				}
				PlayerManager.LocalPlayerScript.playerSync.pullingObject = gameObject;
			}
		}
	}

	public void CancelPullBehaviour()
	{
		if (pulledBy == PlayerManager.LocalPlayer) {
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
			return;
		}
		if (pulledBy != PlayerManager.LocalPlayer) {
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopOtherPulling(gameObject);
		}
		PlayerManager.LocalPlayerScript.playerSync.PullReset(gameObject.GetComponent<NetworkIdentity>().netId);
	}

	public void TryPush(GameObject pushedBy, Vector2 pushDir)
	{
		if (pushDir != Vector2.up && pushDir != Vector2.right && pushDir != Vector2.down && pushDir != Vector2.left) {
			return;
		}
		if (pushing || !isPushable || customNetTransform.isPushing
			|| customNetTransform.predictivePushing) {
			return;
		}

		if (pulledBy != null) {
			if (pulledBy != PlayerManager.LocalPlayer) {
				pulledBy.GetComponent<PlayerNetworkActions>().CmdStopOtherPulling(gameObject);
			} else {
				pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(gameObject);
				PlayerManager.LocalPlayerScript.playerNetworkActions.isPulling = false;
				PlayerManager.LocalPlayerScript.playerSync.pullingObject = null;
			}

			pulledBy = null;

		}

		Vector3Int newPos = Vector3Int.RoundToInt(transform.localPosition + (Vector3)pushDir);

		if (matrix.IsPassableAt(newPos) || matrix.ContainsAt(newPos, gameObject)) {
			//Start the push on the client, then start on the server, the server then tells all other clients to start the push also
			pusher = pushedBy;
			pushTarget = newPos;
			//Start command to push on server
			if (pusher == PlayerManager.LocalPlayer) {
				//pushing for local player is set to true from CNT, to make sure prediction isn't overwhelmed
				customNetTransform.PushToPosition(pushTarget, PlayerManager.LocalPlayerScript.playerMove.speed, this);
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, transform.localPosition, pushTarget, 
				                                                                PlayerManager.LocalPlayerScript.playerMove.speed);
			}
		}
	}


	public void BreakPull()
	{
		PlayerScript player = PlayerManager.LocalPlayerScript;
		if (!player.playerSync) //FIXME: this doesn't exist on the client sometimes
		{
			return;
		}
		GameObject pullingObject = player.playerSync.pullingObject;
		if (!pullingObject || !pullingObject.Equals(gameObject)) {
			return;
		}
		player.playerSync.PullReset(NetworkInstanceId.Invalid);
		player.playerSync.pullingObject = null;
		player.playerSync.pullObjectID = NetworkInstanceId.Invalid;
		player.playerNetworkActions.isPulling = false;
		pulledBy = null;
	}

	//This is to turn off CNT while pulling an object
	[ClientRpc]
	public void RpcToggleCnt(bool activeState){
		if(customNetTransform != null){
			customNetTransform.enabled = activeState;
		}
	}


	private void Update()
	{
		if (pushing) {
			if (customNetTransform.predictivePushing) {
				//Wait for the server to catch up to the pushtarget if predictivePushing is true
				if (Vector3.Distance(currentPos, pushTarget) < 0.1f) {
					//if it is then set it to false, this ensures that the player cannot keep pushing if
					//he is experiencing high lag by waiting for the server position to match up
					customNetTransform.predictivePushing = false;
				}
				return;
			}

			if (Vector3.Distance(transform.localPosition, pushTarget) < 0.1f && !customNetTransform.isPushing) {
				pushing = false;
				customNetTransform.predictivePushing = false;
				registerTile.UpdatePosition();
			}
			if(pushing && customNetTransform.IsFloating() && customNetTransform.IsInSpace()){
				pushing = false;
				customNetTransform.predictivePushing = false;
			}
		}
	}

	private void LateUpdate()
	{
		if (CustomNetworkManager.Instance._isServer) {
			if (transform.hasChanged) {
				transform.hasChanged = false;
				currentPos = transform.localPosition;
			}
		}
	}
}
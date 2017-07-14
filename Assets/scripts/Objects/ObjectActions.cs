using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Matrix;
using PlayGroup;

public class ObjectActions : NetworkBehaviour
{
	public float moveSpeed = 7f;
	public bool allowedToMove = true;
	private RegisterTile registerTile;

	[SyncVar]
	public GameObject pulledBy;
 
	//cache
	private float journeyLength;
	private Vector3 pushTarget;
	private GameObject pusher;
	private bool pushing = false;
	private bool updatingPosSync = false;

	[SyncVar(hook = "PushSync")]
	public Vector3 serverPos;

	[SyncVar] //FIXME hook SetPos
	public Vector3 currentPos;

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
	}

	public override void OnStartClient(){
		if (currentPos != Vector3.zero) {
			transform.position = registerTile.editModeControl.Snap(currentPos);
			registerTile.UpdateTile();
		}
		base.OnStartClient();
	}

	void OnMouseDown()
	{
		if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform)) {
			if (pulledBy == PlayerManager.LocalPlayer) {
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);

				return;
			}
            PlayerManager.LocalPlayerScript.playerSync.PullReset(gameObject.GetComponent<NetworkIdentity>().netId);
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPullObject(gameObject);
		}
	}

	public void TryPush(GameObject pushedBy, float pusherSpeed, Vector2 pushDir)
	{
		if (pushDir != Vector2.up && pushDir != Vector2.right
		    && pushDir != Vector2.down && pushDir != Vector2.left)
			return;
		if (pushing) {
			return;
		}

		if (pulledBy != null) {
			if (CustomNetworkManager.Instance._isServer) {
				pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(gameObject);
			} else {
				pulledBy = null;
			}
		}

		moveSpeed = pusherSpeed;
		Vector3 newPos = RoundedPos(transform.position) + (Vector3)pushDir;
		newPos.z = transform.position.z;
		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			//Start the push on the client, then start on the server, the server then tells all other clients to start the push also

			pusher = pushedBy;
			pusher.GetComponent<PlayerMove>().isPushing = true;
			pushTarget = newPos;
			journeyLength = Vector3.Distance(transform.position, newPos) + 0.2f;
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, pushTarget);
			pushing = true;
		} 
	}

	void Update()
	{
		if (pushing) {
			PushTowards();
		}
	}

	void LateUpdate(){
		if (CustomNetworkManager.Instance._isServer) {
			if (transform.hasChanged) {
				transform.hasChanged = false;
				currentPos = transform.position;
			}
		}
	}

	private void PushTowards()
	{
		transform.position = Vector3.MoveTowards(transform.position, pushTarget, (moveSpeed * Time.deltaTime) * journeyLength);
	
		if(transform.position == pushTarget){
			transform.position = registerTile.editModeControl.Snap(pushTarget);
			registerTile.UpdateTile();

			StartCoroutine(PushFinishWait());
		}
	}

	IEnumerator PushFinishWait(){
		yield return new WaitForSeconds(0.05f);

		if (pusher == PlayerManager.LocalPlayer) {
			pushing = false;
			pusher.GetComponent<PlayerMove>().isPushing = false;

		} else {
			if (transform.position != registerTile.savedPosition) {
				transform.position = registerTile.editModeControl.Snap(pushTarget);
				registerTile.UpdateTile();
			}
			pushing = false;
		}
		pusher = null;
	}
		
//	public void SetPos(Vector3 newPos){
//		transform.position = newPos;
//		registerTile.UpdateTile();
//	}
		
	private void PushSync(Vector3 pos)
	{
		if (!CustomNetworkManager.Instance._isServer && pusher != PlayerManager.LocalPlayer) {
				pushTarget = pos;
				journeyLength = Vector3.Distance(transform.position, pos) + 0.2f;
				pushing = true;
		}
	}
		
	private Vector3 RoundedPos(Vector3 pos)
	{
		return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
	}
}

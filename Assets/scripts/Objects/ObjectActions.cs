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
//	private NetworkTransform networkTransform;

	public GameObject pulledBy;
 
	//cache
	private Vector3 pushTarget;
	private GameObject pusher;
	private Vector2 currentDir;
	private Vector2 headingDir;
	private bool pushing = false;

	[SyncVar(hook="PushSync")]
	private Vector3 serverPos;

	[SyncVar(hook="PosUpdate")]
	private Vector3 currentPos;

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
//		networkTransform = GetComponent<NetworkTransform>();
	}

	void OnMouseDown()
	{
		if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform)) {
			if (pulledBy == PlayerManager.LocalPlayer) {
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
				return;
			}
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
		currentDir = pushDir;
		Vector3 newPos = RoundedPos(transform.position) + (Vector3)currentDir;
		newPos.z = transform.position.z;
		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			pushTarget = newPos;
			pushing = true;
			if (CustomNetworkManager.Instance._isServer) {
				ManualPush(pushTarget);
			}
		} 

	}

	void Update(){
		if (!CustomNetworkManager.Instance._isServer) {
			if (transform.hasChanged) {
				transform.hasChanged = false;
				currentPos = transform.position;
			}
		}
	}
		
	private void PosUpdate(Vector3 _pos){
		if (pulledBy == null) {
			serverPos = _pos;
			transform.position = registerTile.editModeControl.Snap(_pos);
			registerTile.UpdateTile();
		}

	}

	public void ManualPush(Vector3 pos){
		StartCoroutine(WaitForServer());
	}

	private void PushSync(Vector3 pos){
		if (!CustomNetworkManager.Instance._isServer) {
			serverPos = pos;
			transform.position = registerTile.editModeControl.Snap(pos);
			registerTile.UpdateTile();
		}
	}

	IEnumerator WaitForServer(){
		yield return new WaitForEndOfFrame();

		if (CustomNetworkManager.Instance._isServer) {
			transform.position = registerTile.editModeControl.Snap(pushTarget);
			serverPos = transform.position;
			registerTile.UpdateTile();
			pushing = false;
		}else{
			
			while (transform.position != serverPos) {
				yield return new WaitForEndOfFrame();
			}
			pushing = false;

			}
	}

	//TODO use movetowards instead of Snapping: Phase 2
//	private void PushObject()
//	{
//		float journeyLength = Vector3.Distance(transform.position, pushTarget);
//		transform.position = Vector3.MoveTowards(transform.position, pushTarget, (moveSpeed * Time.deltaTime) / journeyLength);
//		headingDir = (Vector2)Vector3.Normalize(pushTarget - transform.position);
//		if (headingDir != currentDir) {
//			canSync = true;
//			checkServerTime = 0f;
//			pushing = false;
//			registerTile.transform.position = pushTarget;
//			registerTile.UpdateTile();
//		}
//	}

	private Vector3 RoundedPos(Vector3 pos)
	{
		return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
	}
}

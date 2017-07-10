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
	private NetworkTransform networkTransform;

	public GameObject pulledBy;
 
	//cache
	private Vector3 pushTarget;
	private GameObject pusher;
	private Vector2 currentDir;
	private Vector2 headingDir;
	private bool pushing = false;
	private bool canSync = false;
	private float checkServerTime = 0f;

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		networkTransform = GetComponent<NetworkTransform>();
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
			Debug.Log("ALREADY PUSH");
			return;
		}

		if (pulledBy != null) {
			if (CustomNetworkManager.Instance._isServer) {
				pulledBy.GetComponent<PlayerNetworkActions>().CmdStopPulling(gameObject);
			} else {
				pulledBy = null;
			}
		}
		pushing = true;
//		networkTransform.enabled = false;
		moveSpeed = pusherSpeed;
		currentDir = pushDir;
		Vector3 newPos = RoundedPos(transform.position) + (Vector3)currentDir;
		newPos.z = transform.position.z;
		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			pushTarget = newPos;
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdTryPush(gameObject, newPos);
			pushing = false;
//			pushing = true;
//			checkServerTime = 0f;
		} else {
			pushing = false;
		}
	}

	[ClientRpc]
	public void RpcPush(Vector3 pos){
		transform.position = registerTile.editModeControl.Snap(pushTarget);
		registerTile.UpdateTile();
		Debug.Log("PUSH PUSH");
	}
		
	void Update()
	{
		if (pushing) {
//			PushObject();
		}
	}

	void LateUpdate(){
		if (canSync) {
			checkServerTime += Time.deltaTime;
			if (checkServerTime > 1f) {
				canSync = false;
				SyncServer();
			}

		}
	}

	private void PushObject()
	{
		float journeyLength = Vector3.Distance(transform.position, pushTarget);
		transform.position = Vector3.MoveTowards(transform.position, pushTarget, (moveSpeed * Time.deltaTime) / journeyLength);
		headingDir = (Vector2)Vector3.Normalize(pushTarget - transform.position);
		if (headingDir != currentDir) {
			canSync = true;
			checkServerTime = 0f;
			pushing = false;
			registerTile.transform.position = pushTarget;
			registerTile.UpdateTile();
		}
	}

	private void SyncServer(){
		networkTransform.enabled = true;
	}

	private Vector3 RoundedPos(Vector3 pos)
	{
		return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
	}
}

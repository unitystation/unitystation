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

	public GameObject pulledBy;
	private PlayerSprites pullingPlayerSprites;
	private PlayerSync playerSync;
 
	//cache
	private Vector3 pushTarget;
	private Vector2 pushDir;
	private bool push = false;

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
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

//	[ClientRpc]
//	public void RpcTryToPush(Vector3 position, float speed)
//	{
//        if(pulledBy){
//            PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
//        }
//		Debug.Log("RpcTryToPush");
//		var v1 = editModeControl.Snap(position);
//		var v2 = editModeControl.Snap(transform.position);
//
//		Vector3 dir = v1 - v2;
//		Vector3 newPos = v2 - dir.normalized;
//		moveSpeed = speed;
//	}
     
	public void TryPush(GameObject pusher, float pusherSpeed)
	{
		if (!allowedToMove || push)
			return;

		if (pulledBy == pusher && CustomNetworkManager.Instance._isServer) {
			pusher.GetComponent<PlayerNetworkActions>().isPulling = false;
            pulledBy = null;
		}

		pullingPlayerSprites = pusher.GetComponent<PlayerSprites>();
		pushDir = pullingPlayerSprites.currentDirection;
		Vector3 newPos = RoundedPos(transform.position) + (Vector3)pushDir;
	
		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
            if (pulledBy != null && CustomNetworkManager.Instance._isServer) {
                PlayerNetworkActions pA = pulledBy.GetComponent<PlayerNetworkActions>();
				pA.isPulling = false;
			}

			playerSync = pusher.GetComponent<PlayerSync>();
			newPos.z = transform.position.z;
			pushTarget = newPos;
			push = true;
		}
	}

	Vector3 _pushTarget(Transform pusher){

		Vector3 newPos = RoundedPos(pusher.position) + (Vector3)(pushDir * 2f);


		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			newPos.z = transform.position.z;
			return newPos;
		}
		return transform.position;
	}
        
	void Update()
	{
		if (push) {
			PushObject();
		}
	}
     
	private void PushObject(){
        transform.position = Vector3.MoveTowards(transform.position, _pushTarget(playerSync.transform), 7f * Time.deltaTime);
		Vector2 heading = (Vector2)Vector3.Normalize(_pushTarget(playerSync.transform) - transform.position);
		if (heading != pushDir) {
			push = false;
            registerTile.editModeControl.Snap(_pushTarget(playerSync.transform));
			registerTile.UpdateTile();
			
		}
	}
        
	private Vector3 RoundedPos(Vector3 pos)
	{
		return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
	}
}

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
	private EditModeControl editModeControl;

	[SyncVar(hook = "OnPulledByChanged")]
	public NetworkInstanceId PulledBy;
	private GameObject pulling;
	private PlayerSprites pullingPlayerSprites;
	private PlayerSync playerSync;

	[SyncVar(hook = "OnSnapped")]
	private Vector3 snapPos;

	//cache
	private Vector3 pullTarget;
	private Vector3 pushTarget;
	private Vector3 pushStart;
	private Vector2 pushDir;
	private float pushTime = 0f;
	private float journeyLength;
	private bool push = false;

	void Awake()
	{
		registerTile = GetComponent<RegisterTile>();
		editModeControl = GetComponent<EditModeControl>();
	}

	void OnMouseDown()
	{
		if (Input.GetKey(KeyCode.LeftControl) && PlayerManager.LocalPlayerScript.IsInReach(transform)) {
			if (pulling == PlayerManager.LocalPlayer) {
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
				return;
			}
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPullObject(gameObject);
		}
	}

	[ClientRpc]
	public void RpcTryToPush(Vector3 position, float speed)
	{
//		PulledBy = NetworkInstanceId.Invalid;
		Debug.Log("RpcTryToPush");
		var v1 = editModeControl.Snap(position);
		var v2 = editModeControl.Snap(transform.position);

		Vector3 dir = v1 - v2;
		Vector3 newPos = v2 - dir.normalized;
		moveSpeed = speed;

//		MoveToTile(newPos);
	}

	public void OnPulledByChanged(NetworkInstanceId pullingId)
	{
		PulledBy = pullingId;
		if (pullingId == NetworkInstanceId.Invalid) {
			pulling = null;
			editModeControl.Snap();
		} else {
			pulling = ClientScene.FindLocalObject(pullingId);
			pullingPlayerSprites = pulling.GetComponent<PlayerSprites>();
			playerSync = pulling.GetComponent<PlayerSync>();
			pulling.transform.hasChanged = false;
			push = false;
		}
	}

	public void TryPush(GameObject pusher, float pusherSpeed)
	{
		if (!allowedToMove || push)
			return;

		if (pulling == pusher && CustomNetworkManager.Instance._isServer) {
			pusher.GetComponent<PlayerNetworkActions>().isPulling = false;
			PulledBy = NetworkInstanceId.Invalid;
			pulling = null;
		}

		pullingPlayerSprites = pusher.GetComponent<PlayerSprites>();
		pushDir = pullingPlayerSprites.currentDirection;
		Vector3 newPos = RoundedPos(transform.position) + (Vector3)pushDir;
	
		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			if (pulling != null && CustomNetworkManager.Instance._isServer) {
				PlayerNetworkActions pA = pulling.GetComponent<PlayerNetworkActions>();
				pA.isPulling = false;
				PulledBy = NetworkInstanceId.Invalid;
			}

			playerSync = pusher.GetComponent<PlayerSync>();
			newPos.z = transform.position.z;
			pushTarget = newPos;
			pushStart = transform.position;
			pushTime = 0f;
			push = true;
		}

//		Debug.Log("PUSHED");
//		ResetJourney();
//		targetPos = newPos;
//		registerTile.UpdateTile(newPos);
	}

	Vector3 _pushTarget(Transform pusher){

		Vector3 newPos = RoundedPos(pusher.position) + (Vector3)(pushDir * 2f);


		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			newPos.z = transform.position.z;
			return newPos;
		}
		return transform.position;
	}

	public void OnSnapped(Vector3 newPos)
	{
		snapPos = newPos;
		transform.position = newPos;
	}

	void Update()
	{
		if (push) {
			PushObject();
		}
		
		if (pulling == null)
			return;

		if (pulling.transform.hasChanged) {
			pulling.transform.hasChanged = false;
			PullCalculate();
			PullObject();
		}
	}

	private void PullCalculate()
	{
		if (pulling == null)
			return;
		
		Vector3 newPos = RoundedPos(pulling.transform.position) - (Vector3)pullingPlayerSprites.currentDirection;
		Vector3 unRoundedPos = pulling.transform.position - (Vector3)pullingPlayerSprites.currentDirection;
		newPos.z = transform.position.z;

		if (Matrix.Matrix.At(newPos).IsPassable() || Matrix.Matrix.At(newPos).ContainsTile(gameObject)) {
			pullTarget = unRoundedPos;
		}
	}

	private void JourneyCalculate(bool isForPull){
		if (pulling == null && !push) {
			return;
			journeyLength = 1f;
		}

		if (isForPull) {
			journeyLength = Vector3.Distance(transform.position, pullTarget);
		} else {
		//Push
			journeyLength = Vector3.Distance(transform.position, _pushTarget(playerSync.transform));
		}
		//Break the pulling action if object is blocked and too far away
		if (journeyLength > 2f) {
			PlayerManager.LocalPlayerScript.playerNetworkActions.CmdStopPulling(gameObject);
		}
	}

	private void PushObject(){
		JourneyCalculate(false);
		transform.position = Vector3.MoveTowards(transform.position, _pushTarget(playerSync.transform), playerSync.currentSpeed / journeyLength);
		Vector2 heading = (Vector2)Vector3.Normalize(_pushTarget(playerSync.transform) - transform.position);
		Debug.Log("heading: " + heading + " pushDir: " + pushDir);
		if (heading != pushDir) {
			push = false;
			registerTile.UpdateTile(RoundedPos(_pushTarget(playerSync.transform)));
			editModeControl.Snap(_pushTarget(playerSync.transform));
		}
	}

	private void PullObject()
	{
		if (pullTarget == Vector3.zero)
			return;
		JourneyCalculate(true);
		//transform.position = Vector3.MoveTowards(transform.position, pullTarget, Mathf.Clamp((moveSpeed / journeyLength) * Time.deltaTime,0.8f,1f));
		transform.position = Vector3.MoveTowards(transform.position, pullTarget, playerSync.currentSpeed/journeyLength);

		if (transform.position == pullTarget) {
			registerTile.UpdateTile(RoundedPos(pullTarget));
		}
	}

	private Vector3 RoundedPos(Vector3 pos)
	{
		return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
	}
}

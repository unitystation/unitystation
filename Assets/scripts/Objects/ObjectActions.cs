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

	[SyncVar(hook = "OnPushed")]
	private Vector3 targetPos;

	[SyncVar(hook = "OnSnapped")]
	private Vector3 snapPos;

	[SyncVar]
	private Vector3 lastPlayerPos;

	void Awake()
	{
		targetPos = transform.position;
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

	public void TryToPush(PlayerMove playerMove)
	{
		PulledBy = NetworkInstanceId.Invalid;

		var v1 = editModeControl.Snap(playerMove.transform.position);
		var v2 = editModeControl.Snap(transform.position);

		Vector3 dir = v1 - v2;
		Vector3 newPos = v2 - dir.normalized;
		moveSpeed = playerMove.speed;

		MoveToTile(newPos);
	}

	public void OnPulledByChanged(NetworkInstanceId pullingId)
	{
		PulledBy = pullingId;
		if (pullingId == NetworkInstanceId.Invalid) {
			pulling = null;
			lastPlayerPos = Vector3.zero;
		} else {
			pulling = ClientScene.FindLocalObject(pullingId);
			lastPlayerPos = pulling.transform.position;
		}
	}

	public void OnPushed(Vector3 newPos)
	{
		targetPos = newPos;
		registerTile.UpdateTile(newPos);
	}

	public void OnSnapped(Vector3 newPos)
	{
		snapPos = newPos;
		transform.position = newPos;
	}

	void MoveToTile(Vector3 tilePos)
	{
		if (!allowedToMove)
			return;
	
		if (Matrix.Matrix.At(tilePos).IsPassable()) {
			tilePos.z = transform.position.z;
			targetPos = tilePos;
		}
	}

	void Update()
	{
		if (pulling != null) {
			PullAction();
		}

		if (transform.position != targetPos) {
			MoveAction();
		}
	}

	private void PullAction()
	{
		if (lastPlayerPos == Vector3.zero || pulling.transform.position == lastPlayerPos)
			return;

		var playerSprites = pulling.GetComponent<PlayerSprites>();
		Vector3 faceDir = playerSprites.currentDirection;
		Vector3 newPos = RoundedPos(pulling.transform.position) - faceDir;
		newPos.z = transform.position.z;

		if (Matrix.Matrix.At(newPos).IsPassable()) {
			targetPos = newPos;
			registerTile.UpdateTile(targetPos);
		}

		lastPlayerPos = RoundedPos(pulling.transform.position);
	}

	private void MoveAction()
	{
		transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
		if (transform.position == targetPos) {
			var newPos = editModeControl.Snap();
			if (isServer) {
				snapPos = newPos;
			}
		}
	}

	private Vector3 RoundedPos(Vector3 pos)
	{
		return new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y), pos.z);
	}
}

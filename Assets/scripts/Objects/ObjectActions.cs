using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Matrix;
using PlayGroup;

public class ObjectActions : MonoBehaviour
{
	private Vector3 targetPos;
	public float moveSpeed = 7f;
	public bool allowedToMove = true;
	private RegisterTile registerTile;
	private EditModeControl editModeControl;
	public GameObject pulledBy;
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
			if (pulledBy == null) {
				pulledBy = PlayerManager.LocalPlayer;
				lastPlayerPos = pulledBy.transform.position;
			} else {
				pulledBy = null;
			}
		}
	}

	public void TryToPush(Vector3 playerPos, float _moveSpeed)
	{
		if (pulledBy != null) {
			pulledBy = null;
		}
		Vector3 dir = playerPos - transform.position;
		Vector3 newPos = transform.position - dir.normalized;
		moveSpeed = _moveSpeed;
		MoveToTile(newPos);
	}

	void MoveToTile(Vector3 tilePos)
	{
		if (!allowedToMove)
			return;
	
		if (Matrix.Matrix.At(tilePos).IsPassable()) {
			tilePos.z = transform.position.z;
			targetPos = tilePos;
			registerTile.UpdateTile(targetPos);
		}
	}

	void Update()
	{
		if (pulledBy != null) {
			PullAction();
		}

		if (transform.position != targetPos) {
			MoveAction();
		}
	}

	private void PullAction(){
		if (pulledBy.transform.position != lastPlayerPos) {
			Vector3 faceDir = PlayerManager.LocalPlayerScript.playerSprites.currentDirection;
			Vector3 newPos = RoundedPos(pulledBy.transform.position) - faceDir;
			newPos.z = transform.position.z;
			if (Matrix.Matrix.At(newPos).IsPassable()) {
				targetPos = newPos;
				registerTile.UpdateTile(targetPos);
			}
			lastPlayerPos = RoundedPos(pulledBy.transform.position);
		}
	}

	private void MoveAction(){
		transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
		if (transform.position == targetPos) {
			editModeControl.Snap();
		}
	}

	private Vector3 RoundedPos(Vector3 pos){
		Vector3 snapPos = new Vector3(Mathf.Round(pos.x),Mathf.Round(pos.y),pos.z);
		return snapPos;
	}
}

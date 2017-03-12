﻿using System.Collections;
using UnityEngine;

namespace PlayGroup
{
	public class PlayerMove : MonoBehaviour
	{
		[HideInInspector]
		public bool isMoving = false;
		[HideInInspector]
		public bool lerpA = false;
		private bool tryOpenDoor = false;
		[HideInInspector]
		public Vector2 moveDirection;
		private IntVector2 nextTile;

		private Vector3 startPos;
		private Vector3 node;
		private Vector3 toClamp;

		public float lerpSpeed = 10f;
		[HideInInspector]
		private float lerpTime = 0f;
		[HideInInspector]
		public float clampPos;
		public float moveSpeed;

		private PlayerScript playerScript;

		void Start()
		{
			playerScript = GetComponent<PlayerScript>();
		}

		void Update()
		{
			if (lerpA) {
				LerpToTarget();
			}
		}

		void FixedUpdate()
		{
			if (isMoving) {
				//Move character via Transform
				MoveCharacter();
			}
		}

		//move in direction input
		public void MoveInDirection(Vector2 direction)
		{ 
			moveDirection = direction;

			if (AllowedMove()) {
				lerpA = false;
				isMoving = true;

				if (direction == Vector2.right || direction == Vector2.left) {
					clampPos = Mathf.Round(transform.position.y); 
				}

				if (direction == Vector2.down || direction == Vector2.up) {
					clampPos = Mathf.Round(transform.position.x);
				}
			} else {
				//Check why it is not passable (doors etc)
				DoorController getDoor = Matrix.Matrix.At(nextTile.x, nextTile.y).GetDoor();
				if (getDoor != null && !tryOpenDoor) {
					tryOpenDoor = true;
					StartCoroutine(OpenCoolDown());
					getDoor.TryOpen();
				} 
			}
		}

		private bool AllowedMove()
		{
			if (moveDirection == Vector2.right) {
				nextTile.x = (int)(Mathf.Floor(transform.position.x) + moveDirection.x); 
				nextTile.y = (int)(Mathf.Round(transform.position.y) + moveDirection.y);
			} else if (moveDirection == Vector2.left) {
				nextTile.x = (int)(Mathf.Ceil(transform.position.x) + moveDirection.x); 
				nextTile.y = (int)(Mathf.Round(transform.position.y) + moveDirection.y);
			} else if (moveDirection == Vector2.up) {
				nextTile.x = (int)(Mathf.Round(transform.position.x) + moveDirection.x); 
				nextTile.y = (int)(Mathf.Floor(transform.position.y) + moveDirection.y);
			} else if (moveDirection == Vector2.down) {
				nextTile.x = (int)(Mathf.Round(transform.position.x) + moveDirection.x); 
				nextTile.y = (int)(Mathf.Ceil(transform.position.y) + moveDirection.y);
			}
			return Matrix.Matrix.At(nextTile.x, nextTile.y).IsPassable();
		}

		private void MoveCharacter()
		{
			if (AllowedMove()) {
				if (moveDirection == Vector2.down) {
					transform.Translate(0f, moveDirection.y * moveSpeed, 0);
					SetClamp(transform.position, true);
					return;
				}
				if (moveDirection == Vector2.up) {
					transform.Translate(0f, moveDirection.y * moveSpeed, 0);
					SetClamp(transform.position, true);
					return;
				}
				if (moveDirection == Vector2.right) {
					transform.Translate(moveDirection.x * moveSpeed, 0f, 0);
					SetClamp(transform.position, false);
					return;
				}
				if (moveDirection == Vector2.left) {
					transform.Translate(moveDirection.x * moveSpeed, 0f, 0);
					SetClamp(transform.position, false);
					return;
				}
			}
		}

		private void SetClamp(Vector3 _toClamp, bool clampX)
		{
			toClamp = _toClamp;
			if (clampX) {
				if (toClamp.x != clampPos) {
					toClamp.x = clampPos;
				}
			} else {
				if (toClamp.y != clampPos) {
					toClamp.y = clampPos;
				}
			}
			transform.position = toClamp;
		}

		//movement input stopped, only use this to snap if there is an applied velocity
		public void MoveInputReleased()
		{
			startPos = transform.position;
			node = Matrix.Matrix.GetClosestNode(transform.position, playerScript.playerSprites.currentDirection);

			//if target !isPassable then snap back to currentTile
			if (node.x == Mathf.Round(startPos.x) && node.y == Mathf.Round(startPos.y)) {
				transform.position = node;
				isMoving = false;
			} else {
				//else lerp to the target tile depending on direction travelling
				lerpTime = 0f;
				lerpA = true;
			}
		}

		//used with LerpA in update
		private void LerpToTarget()
		{
			lerpTime += Time.deltaTime;
			float t = lerpTime * lerpSpeed;

			transform.position = Vector2.Lerp(startPos, node, t);

			if (moveDirection == Vector2.right && transform.position.x >= node.x) {
				ResetParameters();
			}
			if (moveDirection == Vector2.left && transform.position.x <= node.x) {
				ResetParameters();
			}
			if (moveDirection == Vector2.up && transform.position.y >= node.y) {
				ResetParameters();
			}
			if (moveDirection == Vector2.down && transform.position.y <= node.y) {
				ResetParameters();
			}
			if (moveDirection == Vector2.zero && transform.position == node) {
				ResetParameters();
			}	
		}

		private void ResetParameters()
		{
			isMoving = false;
			lerpA = false;
		}

		IEnumerator OpenCoolDown()
		{
			yield return new WaitForSeconds(1f);
			tryOpenDoor = false;
		}
	}
}

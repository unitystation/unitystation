using UnityEngine;
using System.Collections;

namespace PlayGroup
{
	public struct IntVector2
	{
		public int x;
		public int y;
	}
	[RequireComponent(typeof(Rigidbody2D))]
	public class PhysicsMove: MonoBehaviour
	{

		private Rigidbody2D thisRigi;

		public bool isMoving = false;
		public bool lerpA = false;
		public bool isSpaced = false;
		public bool triedToMoveInSpace = false;
		public bool keyDown = false;

		private Vector2 moveDirection;

		public Vector2 MoveDirection { get { return moveDirection; } }

		private Vector3 startPos;
		private Vector3 node;
		private Vector3 toClamp;
		private IntVector2 nextTile;

		public float lerpSpeed = 10f;
		private float lerpTime = 0f;
		public float clampPos;
		public float moveSpeed;

		private bool tryOpenDoor = false;

		void Start()
		{
			thisRigi = GetComponent<Rigidbody2D>();
		}

		void Update()
		{
			//when movekeys are released then take the character to the nearest node
			if (lerpA) {
				LerpToTarget();
			}
		}

		void FixedUpdate()
		{
			if (isMoving) {
				//Move character via RigidBody
				MoveRigidBody();
			}
		}

		//move in direction input
		public void MoveInDirection(Vector2 direction)
		{ 
			nextTile.x = (int)(Mathf.Round(transform.position.x) + direction.x); 
			nextTile.y = (int)(Mathf.Round(transform.position.y) + direction.y);

			if (Matrix.Matrix.At(nextTile.x, nextTile.y).IsPassable()) {
				lerpA = false;
				moveDirection = direction;
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
					StartCoroutine("OpenCoolDown");
					getDoor.CmdTryOpen();
				} 
			}
		}

		//force a snap to the tile manually
		public void ForceSnapToTile()
		{
			startPos = transform.position;
			moveDirection = Vector2.zero;
			node = new Vector3(Mathf.Round(transform.position.x), Mathf.Round(transform.position.y), 0f);
			if (thisRigi != null)
				thisRigi.velocity = Vector3.zero;

			lerpTime = 0f;
			lerpA = true;
		}

		//movement input stopped, only use this to snap if there is an applied velocity
		public void MoveInputReleased()
		{
			startPos = transform.position;
			node = Matrix.Matrix.GetClosestNode(transform.position, thisRigi.velocity);

			//if target !isPassable then snap back to currentTile
			if (node.x == Mathf.Round(startPos.x) && node.y == Mathf.Round(startPos.y)) {
				thisRigi.MovePosition(node);
				isMoving = false;
				thisRigi.velocity = Vector3.zero;
			} else {
				//else lerp to the target tile depending on direction travelling
				thisRigi.velocity = Vector3.zero;
				lerpTime = 0f;
				lerpA = true;
			}
		}

		//Most used for Shuffling Players out of tiles when being occupied by others
	
		public void ManualMoveToTarget(Vector3 targetPos)
		{
			startPos = transform.position;
			node = targetPos;
			thisRigi.velocity = Vector3.zero;
			lerpTime = 0f;
			lerpA = true;
		}

		//used with LerpA in update
		private void LerpToTarget()
		{
			lerpTime += Time.deltaTime;
			float t = lerpTime * lerpSpeed;

			thisRigi.MovePosition(Vector2.Lerp(startPos, node, t));

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

		private void ResetParameters(){
			isMoving = false;
			lerpA = false;
			thisRigi.velocity = Vector3.zero;
		}

		//move the character via RigidBody in FixedUpdate
		private void MoveRigidBody()
		{
			if (!triedToMoveInSpace) {
				if (moveDirection == Vector2.down) {
					thisRigi.velocity = new Vector3(0f, moveDirection.y, 0).normalized * moveSpeed;
					SetClamp(transform.position, true);
					return;
				}
				if (moveDirection == Vector2.up) {
					thisRigi.velocity = new Vector3(0f, moveDirection.y, 0).normalized * moveSpeed;
					SetClamp(transform.position, true);
					return;
				}
				if (moveDirection == Vector2.right) {
					thisRigi.velocity = new Vector3(moveDirection.x, 0f, 0).normalized * moveSpeed;
					SetClamp(transform.position, false);
					return;
				}
				if (moveDirection == Vector2.left) {
					thisRigi.velocity = new Vector3(moveDirection.x, 0f, 0).normalized * moveSpeed;
					SetClamp(transform.position, false);
					return;
				}
			}
		}

		private void SetClamp(Vector3 _toClamp, bool clampX){
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

		IEnumerator OpenCoolDown(){
			yield return new WaitForSeconds(1f);
			tryOpenDoor = false;
		}
	}
}

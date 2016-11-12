using UnityEngine;
using System.Collections;
using SS.GameLogic;


namespace SS.PlayGroup{
	
	[RequireComponent (typeof (Rigidbody2D))]

public class PhysicsMove : MonoBehaviour {

		public GameManager gameManager;
		public GameManager.Direction direction;

		private Rigidbody2D thisRigi;

		public bool isMoving = false;
		public bool lerpA = false;
		public bool isSpaced = false;
		public bool triedToMoveInSpace = false;
		public bool keyDown = false;

		public Vector2 moveDirection;
		private Vector3 startPos;
		public Vector3 node;
		private Vector3 toClamp;

		public float lerpSpeed = 7f;
		private float lerpTime = 0f;
		public float clampPos;
		public float moveSpeed;


	// Use this for initialization
	void Start () {
			thisRigi = GetComponent<Rigidbody2D> ();
			direction = GameManager.Direction.Down;
			GameObject findGM = GameObject.FindGameObjectWithTag ("GameManager");
			if (findGM != null) {
				gameManager = findGM.GetComponent<GameManager> ();
				//TODO error handling
			
			}
	}
	
	// Update is called once per frame
	void Update () {

			//when movekeys are released then take the character to the nearest node
			if (lerpA) {
				LerpToTarget ();
			}
	
	}

	void FixedUpdate(){

			if (isMoving) {
				//Move character via RigidBody
				MoveRigidBody ();

			} 
	}

		//movement input stopped
		public void MoveInputReleased(){ 

			startPos = transform.position;
			node = gameManager.GetClosestNode (transform.position, thisRigi.velocity);
			thisRigi.velocity = Vector3.zero;
			lerpTime = 0f;
			lerpA = true;

		}

		//used with LerpA in update
		private void LerpToTarget(){

			lerpTime += Time.deltaTime;
			float t = lerpTime * lerpSpeed;

			transform.position = Vector3.Lerp (startPos, node, t);
			if (direction == GameManager.Direction.Right && transform.position.x >= node.x) {
				isMoving = false;
				lerpA = false;
				thisRigi.velocity = Vector3.zero;
			}
			if (direction == GameManager.Direction.Left && transform.position.x <= node.x) {
				isMoving = false;
				lerpA = false;
				thisRigi.velocity = Vector3.zero;
			}
			if (direction == GameManager.Direction.Up && transform.position.y >= node.y) {
				isMoving = false;
				lerpA = false;
				thisRigi.velocity = Vector3.zero;
			}
			if (direction == GameManager.Direction.Down && transform.position.y <= node.y) {
				isMoving = false;
				lerpA = false;
				thisRigi.velocity = Vector3.zero;
			}
		}

		//move the character via RigidBody in FixedUpdate
		private void MoveRigidBody(){

			if (!triedToMoveInSpace) {

				if (direction == GameManager.Direction.Down) {
					thisRigi.velocity = new Vector3 (0f, moveDirection.y, 0).normalized * moveSpeed;
					toClamp = transform.position;
					Mathf.Clamp (toClamp.x, clampPos, clampPos);
					transform.position = toClamp;
					return;
				}
				if (direction == GameManager.Direction.Up) {
					thisRigi.velocity = new Vector3 (0f, moveDirection.y, 0).normalized * moveSpeed;
					toClamp = transform.position;
					Mathf.Clamp (toClamp.x, clampPos, clampPos);
					transform.position = toClamp;
					return;
				}
				if (direction == GameManager.Direction.Right) {
					thisRigi.velocity = new Vector3 (moveDirection.x, 0f, 0).normalized * moveSpeed;
					toClamp = transform.position;
					Mathf.Clamp (toClamp.y, clampPos, clampPos);
					transform.position = toClamp;
					return;
				}
				if (direction == GameManager.Direction.Left) {
					thisRigi.velocity = new Vector3 (moveDirection.x, 0f, 0).normalized * moveSpeed;
					toClamp = transform.position;
					Mathf.Clamp (toClamp.y, clampPos, clampPos);
					transform.position = toClamp;
					return;
				}
			}
			if (isSpaced && !triedToMoveInSpace) {
				triedToMoveInSpace = true;
				thisRigi.mass = 0f;
				thisRigi.drag = 0f;
				thisRigi.angularDrag = 0f;

			}

		}
}
}

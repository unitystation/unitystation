using UnityEngine;
using System.Collections.Generic;
using MovementEffects;


public class Camera2DFollow : MonoBehaviour {

	public static Camera2DFollow followControl;

	public Transform target;

	public float damping = 1;

	public float lookAheadFactor = 3;
	private float lookAheadSave;
	public float lookAheadReturnSpeed = 0.5f;
	public float lookAheadMoveThreshold = 0.1f;
	public float yOffSet = 0f;

	bool isSearching = false;

	float offsetZ;
	Vector3 lastTargetPosition;
	Vector3 currentVelocity;
	Vector3 lookAheadPos;

	float nextTimeToSearch = 0;


		private bool zooming = false;
		private float newZpos;
		private float zoomSpeed;
		private float zoomLerp = 0f;
		private float fromZ;

		// Y Pos Lerp
		private bool yLerp = false;
		private float newYposRestrict;
		private float newYLerpSpeed;
		private float yLerpVal;
		private float fromY;




	void Awake() {

				if (followControl == null) {
				
						followControl = this;
				
				}
	}
	
	// Use this for initialization
	void Start () {

				lookAheadSave = lookAheadFactor;
				if (target != null) {
						lastTargetPosition = target.position;
						offsetZ = (transform.position - target.position).z;
				}
		transform.parent = null;

			
	}
	
	
		void Update(){

		//Searching stuff
		if (PlayerScript.playerControl == null && !isSearching) {
						isSearching = true;
//						Debug.Log ("PLAYER LOST");
						Timing.RunCoroutine (FindPlayer());
						return;
					
				}
		}

		void LateUpdate(){
			

				if (target != null) {

						// only update lookahead pos if accelerating or changed direction
						float xMoveDelta = (target.position - lastTargetPosition).x;

						bool updateLookAheadTarget = Mathf.Abs (xMoveDelta) > lookAheadMoveThreshold;

						if (updateLookAheadTarget) {
								lookAheadPos = lookAheadFactor * Vector3.right * Mathf.Sign (xMoveDelta);
						} else {
								lookAheadPos = Vector3.MoveTowards (lookAheadPos, Vector3.zero, Time.deltaTime * lookAheadReturnSpeed);	
						}
			
						Vector3 aheadTargetPos = target.position + lookAheadPos + Vector3.forward * offsetZ;
						aheadTargetPos.y += yOffSet;
						Vector3 newPos = Vector3.SmoothDamp (transform.position, aheadTargetPos, ref currentVelocity, damping);


						transform.position = newPos;
		
						lastTargetPosition = target.position;	
				
				}



	}


		public void LookAheadTemp(float newLookAhead){

				lookAheadFactor = newLookAhead;
				Timing.RunCoroutine (LookAheadSwitch());
		}

		//COROUTINES
		IEnumerator<float> LookAheadSwitch(){

				yield return Timing.WaitForSeconds (2f);
				lookAheadFactor = lookAheadSave;

		}

		IEnumerator<float> FindPlayer(){

			yield return 0f;

		if (PlayerScript.playerControl != null) {
						
			            target = PlayerScript.playerControl.gameObject.transform;
						
						}
		
						isSearching = false;

	}

}

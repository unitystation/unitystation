using UnityEngine;
using System.Collections;
using Game;
using UI;

namespace PlayGroup{
public class PlayerScript : MonoBehaviour {
	public static PlayerScript playerControl;  //TODO remove this from being a singleton as we need multiple instances for photon
		                                       //TODO we will implement a 'isMine' bool and seperate the controls from this object
		                                       //TODO then on spawn, set one instance of PlayerScript as isMine and the rest will be the other network players
			
	public float moveSpeed = 0.1f;

	[HideInInspector]
	public PhysicsMove physicsMove;
	[HideInInspector]
	public PlayerSprites playerSprites;

		[Header("Temp Player Preferences - start sprites (facingDown)")]
	public int bodyNumber;
	public int suitNumber;
	public int beltNumber;
	public int headNumber;
	public int shoesNumber;
	public int underWearNumber;
	public int uniformNumber;

	void Awake(){

		if (playerControl == null) {
		
			playerControl = this;
		
		} else {
		
			Destroy (this);
		
		}

	}

	void Start () {

			//add physics move component and set default movespeed
		physicsMove = gameObject.AddComponent<PhysicsMove> ();
		physicsMove.moveSpeed = moveSpeed;

			//Add player sprite controller component
		playerSprites = gameObject.AddComponent<PlayerSprites> ();
			Invoke ("SetPlayerPrefs", 0.1f);

		//SPAWN POSITION
	
		
		
	}


	void Update () {

			//TODO input needs to be handled in one of the managers to prepare for photon implementation

		if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.A) || Input.GetKeyUp (KeyCode.S) || Input.GetKeyUp (KeyCode.D)) {
				physicsMove.MoveInputReleased ();
		}

		//hold key down inputs. clampPos is used to snap player to an axis on movement
			if (Input.GetKey (KeyCode.D) && !physicsMove.isMoving || Input.GetKey (KeyCode.D) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.left) {
					//RIGHT
					MovePlayer (Vector2.right);

		} 
			if (Input.GetKey (KeyCode.A) && !physicsMove.isMoving || Input.GetKey (KeyCode.A) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.right) {
			//LEFT
				MovePlayer (Vector2.left);

		}
			if (Input.GetKey (KeyCode.S) && !physicsMove.isMoving || Input.GetKey (KeyCode.S) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.up) {
			//DOWN
				MovePlayer (Vector2.down);

		} 
			if (Input.GetKey (KeyCode.W) && !physicsMove.isMoving || Input.GetKey (KeyCode.W) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.down) {
				MovePlayer (Vector2.up);

					} 
				
	}

		void MovePlayer(Vector2 direction){
			if (!UIManager.control.chatControl.chatInputWindow.activeSelf) { //At the moment it just checks if the input window is open and if it is false then allow move
			
			physicsMove.MoveInDirection (direction); //Tile based physics move
			playerSprites.FaceDirection (direction); //Handles the playersprite change on direction change
		
		}
		}

		//Temp
		void SetPlayerPrefs(){

			CustomPlayerPrefs newPrefs = new CustomPlayerPrefs ();
			newPrefs.body = bodyNumber;
			newPrefs.suit = suitNumber;
			newPrefs.belt = beltNumber;
			newPrefs.head = headNumber;
			newPrefs.shoes = shoesNumber;
			newPrefs.underWear = underWearNumber;
			newPrefs.uniform = uniformNumber;

			playerSprites.SetSprites (newPrefs);
		}

	}


	
}

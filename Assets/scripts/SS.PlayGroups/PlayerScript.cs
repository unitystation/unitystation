using UnityEngine;
using System.Collections;
using SS.GameLogic;

namespace SS.PlayGroup{
public class PlayerScript : MonoBehaviour {
	public static PlayerScript playerControl;
			
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
		SetPlayerPrefs ();

		//SPAWN POSITION
		Vector2 newPos = new Vector2 (22f, 32f);
		transform.position = newPos;
		
		
		
	}


	void Update () {


		if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.A) || Input.GetKeyUp (KeyCode.S) || Input.GetKeyUp (KeyCode.D)) {

				physicsMove.MoveInputReleased ();
		}

		//hold key down inputs. clampPos is used to snap player to an axis on movement
			if (Input.GetKey (KeyCode.D) && !physicsMove.isMoving || Input.GetKey (KeyCode.D) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.left) {
			//RIGHT
			physicsMove.MoveInDirection (Vector2.right);
			playerSprites.FaceDirection (Vector2.right);

		} 
			if (Input.GetKey (KeyCode.A) && !physicsMove.isMoving || Input.GetKey (KeyCode.A) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.right) {
			//LEFT
			physicsMove.MoveInDirection (Vector2.left);
			playerSprites.FaceDirection (Vector2.left);

		}
			if (Input.GetKey (KeyCode.S) && !physicsMove.isMoving || Input.GetKey (KeyCode.S) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.up) {
			//DOWN
			physicsMove.MoveInDirection (Vector2.down);
		    playerSprites.FaceDirection (Vector2.down);

		} 
			if (Input.GetKey (KeyCode.W) && !physicsMove.isMoving || Input.GetKey (KeyCode.W) && physicsMove.isMoving && physicsMove._moveDirection == Vector2.down) {
			physicsMove.MoveInDirection (Vector2.up);
			playerSprites.FaceDirection (Vector2.up);

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

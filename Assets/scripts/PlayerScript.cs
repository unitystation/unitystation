using UnityEngine;
using System.Collections.Generic;
using MovementEffects;
using SS.GameLogic;

namespace SS.PlayGroup{
public class PlayerScript : MonoBehaviour {
	public static PlayerScript playerControl;
			
	public float moveSpeed = 0.1f;

	private SpriteRenderer playerRend;
	private SpriteRenderer suitRend;
	private SpriteRenderer beltRend;
	private SpriteRenderer feetRend;
	private SpriteRenderer headRend;
	private SpriteRenderer faceRend;
	private SpriteRenderer maskRend;
	private SpriteRenderer underwearRend;
	private SpriteRenderer uniformRend;

	private Sprite[] playerSheet;
	private Sprite[] suitSheet;
	private Sprite[] beltSheet;
	private Sprite[] feetSheet;
	private Sprite[] headSheet;
	private Sprite[] faceSheet;
	private Sprite[] maskSheet;
	private Sprite[] underwearSheet;
	private Sprite[] uniformSheet;

	[HideInInspector]
	public PhysicsMove physicsMove;

	private float moveHorizontal;
	private float moveVertical;
	


	void Awake(){

		if (playerControl == null) {
		
			playerControl = this;
		
		} else {
		
			Destroy (this);
		
		}

	}

	void Start () {
		physicsMove = gameObject.AddComponent<PhysicsMove> ();
		physicsMove.moveSpeed = moveSpeed;
		//SPAWN POSITION
		Vector2 newPos = new Vector2 (22f, 32f);
		transform.position = newPos;
		
		playerRend = GetComponent<SpriteRenderer>();
	
	
		Timing.RunCoroutine (LoadSpriteSheets ()); //load sprite sheet resources
	}


	void Update () {

		//movement keys down WASD
		if (!physicsMove.isMoving) {
			moveHorizontal = Input.GetAxisRaw ("Horizontal");
			moveVertical = Input.GetAxisRaw ("Vertical");

		}
	
		if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.A) || Input.GetKeyUp (KeyCode.S) || Input.GetKeyUp (KeyCode.D)) {

				physicsMove.MoveInputReleased ();
		}

		//hold key down inputs. clampPos is used to snap player to an axis on movement
			if (Input.GetKey (KeyCode.D) && !physicsMove.isMoving || Input.GetKey (KeyCode.D) && physicsMove.isMoving && physicsMove.direction == GameManager.Direction.Left) {
			//RIGHT
			MoveInDirection (38, Vector2.right);
				physicsMove.clampPos = transform.position.y;
		} 
			if (Input.GetKey (KeyCode.A) && !physicsMove.isMoving || Input.GetKey (KeyCode.A) && physicsMove.isMoving && physicsMove.direction == GameManager.Direction.Right) {
			//LEFT
			MoveInDirection (39, Vector2.left);
				physicsMove.clampPos = transform.position.y;
		}
			if (Input.GetKey (KeyCode.S) && !physicsMove.isMoving || Input.GetKey (KeyCode.S) && physicsMove.isMoving && physicsMove.direction == GameManager.Direction.Up) {
			//DOWN
			MoveInDirection (36, Vector2.down);
				physicsMove.clampPos = transform.position.x;
		} 
			if (Input.GetKey (KeyCode.W) && !physicsMove.isMoving || Input.GetKey (KeyCode.W) && physicsMove.isMoving && physicsMove.direction == GameManager.Direction.Down) {
			MoveInDirection (37, Vector2.up);
				physicsMove.clampPos = transform.position.x;
					} 



	}



	//turning character input and sprite update
	private void MoveInDirection(int playerSprite, Vector2 dir){
		physicsMove.lerpA = false;
		playerRend.sprite = playerSheet [playerSprite];
		suitRend.sprite = suitSheet [playerSprite + 200];
		beltRend.sprite = beltSheet [playerSprite + 26];
		headRend.sprite = headSheet [playerSprite + 185];
		feetRend.sprite = feetSheet [playerSprite];
		underwearRend.sprite = underwearSheet [playerSprite + 16];
		uniformRend.sprite = uniformSheet [playerSprite - 20];

		physicsMove.moveDirection = dir;
		physicsMove.direction = physicsMove.gameManager.GetDirection (dir);
		physicsMove.isMoving = true;
	}





	//COROUTINES

	IEnumerator<float> LoadSpriteSheets(){

		foreach(SpriteRenderer child in this.GetComponentsInChildren<SpriteRenderer>())
		{

			switch (child.name)
			{
			case "suit":
				suitRend = child;
				break;
			case "belt":
				beltRend = child;
				break;
			case "feet":
				feetRend = child;
				break;
			case "head":
				headRend = child;
				break;
			case "face":
				faceRend = child;
				break;
			case "mask":
				maskRend = child;
				break;
			case "underwear":
				underwearRend = child;
				break;
			case "uniform":
				uniformRend = child;
				break;
			}

		}
		playerSheet = Resources.LoadAll<Sprite>("mobs/human");
		suitSheet = Resources.LoadAll<Sprite>("mobs/suit");
		beltSheet = Resources.LoadAll<Sprite>("mobs/belt");
		feetSheet = Resources.LoadAll<Sprite>("mobs/feet");
		headSheet = Resources.LoadAll<Sprite>("mobs/head");
		faceSheet = Resources.LoadAll<Sprite>("mobs/human_face");
		maskSheet = Resources.LoadAll<Sprite>("mobs/mask");
		underwearSheet = Resources.LoadAll<Sprite>("mobs/underwear");
		uniformSheet = Resources.LoadAll<Sprite>("mobs/uniform");



		yield return 0f;
	}
	}	
}

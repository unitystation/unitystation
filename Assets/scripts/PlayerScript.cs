using UnityEngine;
using System.Collections.Generic;
using MovementEffects;
using SS.GameLogic;

public class PlayerScript : MonoBehaviour {
	public static PlayerScript playerControl;

	public GameManager gameManager;
	public float panSpeed = 10.0f;
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

	public bool isMoving = false;
	private Rigidbody2D thisRigi;
	private Vector2 moveDirection;

	public bool isSpaced = false;
	public bool triedToMoveInSpace = false;

	private float lerpTime = 0f;
	public bool lerpA = false;
	private Vector3 startPos;
	public Vector3 node;
	public float lerpSpeed;
	public GameManager.Direction direction;
	private Vector3 toClamp;
	private float clampPos;
	private float moveHorizontal;
	private float moveVertical;
	private bool keyDown = false;


	void Awake(){

		if (playerControl == null) {
		
			playerControl = this;
		
		} else {
		
			Destroy (this);
		
		}

	}

	void Start () {

		//SPAWN POSITION
		Vector2 newPos = new Vector2 (22f, 32f);
		transform.position = newPos;
		thisRigi = GetComponent<Rigidbody2D> ();
		playerRend = GetComponent<SpriteRenderer>();
		direction = GameManager.Direction.Down;
	
		Timing.RunCoroutine (LoadSpriteSheets ()); //load sprite sheet resources
	}


	void Update () {

		//movement keys down WASD
		if (!isMoving) {
			moveHorizontal = Input.GetAxisRaw ("Horizontal");
			moveVertical = Input.GetAxisRaw ("Vertical");

		}
	
		if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.A) || Input.GetKeyUp (KeyCode.S) || Input.GetKeyUp (KeyCode.D)) {

			MoveKeysReleased ();
		}
			
		//hold key down inputs. clampPos is used to snap player to an axis on movement
		if (Input.GetKey (KeyCode.D) && !isMoving || Input.GetKey (KeyCode.D) && isMoving && direction == GameManager.Direction.Left) {
			//RIGHT
			MoveInDirection (38, Vector2.right);
			clampPos = transform.position.y;
		} 
		if (Input.GetKey (KeyCode.A) && !isMoving || Input.GetKey (KeyCode.A) && isMoving && direction == GameManager.Direction.Right) {
			//LEFT
			MoveInDirection (39, Vector2.left);
			clampPos = transform.position.y;
		}
		if (Input.GetKey (KeyCode.S) && !isMoving || Input.GetKey (KeyCode.S) && isMoving && direction == GameManager.Direction.Up) {
			//DOWN
			MoveInDirection (36, Vector2.down);
			clampPos = transform.position.x;
		} 
		if (Input.GetKey (KeyCode.W) && !isMoving || Input.GetKey (KeyCode.W) && isMoving && direction == GameManager.Direction.Down) {
			MoveInDirection (37, Vector2.up);
			clampPos = transform.position.x;
					} 

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
	private void MoveKeysReleased(){ 
		
		startPos = transform.position;
		node = gameManager.GetClosestNode (transform.position, thisRigi.velocity);
		thisRigi.velocity = Vector3.zero;
		lerpTime = 0f;
		lerpA = true;

	}

	//turning character input and sprite update
	private void MoveInDirection(int playerSprite, Vector2 dir){
		lerpA = false;
		playerRend.sprite = playerSheet [playerSprite];
		suitRend.sprite = suitSheet [playerSprite + 200];
		beltRend.sprite = beltSheet [playerSprite + 26];
		headRend.sprite = headSheet [playerSprite + 185];
		feetRend.sprite = feetSheet [playerSprite];
		underwearRend.sprite = underwearSheet [playerSprite + 16];
		uniformRend.sprite = uniformSheet [playerSprite - 20];

		moveDirection = dir;
		direction = gameManager.GetDirection (dir);
		isMoving = true;
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

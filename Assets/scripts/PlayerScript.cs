using UnityEngine;
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

	public int gridX;
	public int gridY;

	//SNAPPING
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

		gridX = 22;
		gridY = 32;
		Vector2 newPos = new Vector2 (22f, 32f);
		transform.position = newPos;
		thisRigi = GetComponent<Rigidbody2D> ();
		playerRend = GetComponent<SpriteRenderer>();
		direction = GameManager.Direction.Down;
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
	}


	void Update () {

		if (Input.GetKeyUp (KeyCode.W) || Input.GetKeyUp (KeyCode.A) || Input.GetKeyUp (KeyCode.S) || Input.GetKeyUp (KeyCode.D)) {
		

				startPos = transform.position;
				node = gameManager.GetClosestNode (transform.position, thisRigi.velocity);
				thisRigi.velocity = Vector3.zero;
				lerpTime = 0f;
				lerpA = true;

		}

		if (!isMoving) {
			moveHorizontal = Input.GetAxisRaw ("Horizontal");
			moveVertical = Input.GetAxisRaw ("Vertical");
		
		}



		if (Input.GetKey (KeyCode.D) && !isMoving) {
			//RIGHT
			playerRend.sprite = playerSheet [38];
			suitRend.sprite = suitSheet [238];
			beltRend.sprite = beltSheet [64];
			headRend.sprite = headSheet [223];
			feetRend.sprite = feetSheet [38];
			underwearRend.sprite = underwearSheet [54];
			uniformRend.sprite = uniformSheet [18];
			direction = GameManager.Direction.Right;
			moveDirection = new Vector2 (1f, 0f);
			clampPos = transform.position.y;
			isMoving = true;
		} 
		if (Input.GetKey (KeyCode.A) && !isMoving) {
			//LEFT
			playerRend.sprite = playerSheet [39];
			suitRend.sprite = suitSheet [239];
			beltRend.sprite = beltSheet [65];
			headRend.sprite = headSheet [224];
			feetRend.sprite = feetSheet [39];
			underwearRend.sprite = underwearSheet [55];
			uniformRend.sprite = uniformSheet [19];
			direction = GameManager.Direction.Left;
			moveDirection = new Vector2 (-1f, 0f);
				clampPos = transform.position.y;
				isMoving = true;
		}
		if (Input.GetKey (KeyCode.S) && !isMoving) {
			playerRend.sprite = playerSheet [36];
			suitRend.sprite = suitSheet [236];
			beltRend.sprite = beltSheet [62];
			headRend.sprite = headSheet [221];
			feetRend.sprite = feetSheet [36];
			underwearRend.sprite = underwearSheet [52];
			uniformRend.sprite = uniformSheet [16];
			direction = GameManager.Direction.Down;
			moveDirection = new Vector2 (0f, -1f);
					clampPos = transform.position.x;
					isMoving = true;
		} 
		if (Input.GetKey (KeyCode.W) && !isMoving) {
			playerRend.sprite = playerSheet [37];
			suitRend.sprite = suitSheet [237];
			beltRend.sprite = beltSheet [63];
			headRend.sprite = headSheet [222];
			feetRend.sprite = feetSheet [37];
			underwearRend.sprite = underwearSheet [53];
			uniformRend.sprite = uniformSheet [17];
			direction = GameManager.Direction.Up;
			moveDirection = new Vector2 (0f, 1f);
						clampPos = transform.position.x;
			isMoving = true;

					} 
			

		if (lerpA) {
			
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

	}

	void FixedUpdate(){


		if (isMoving) {

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

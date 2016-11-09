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

	private bool isMoving = false;
	private Rigidbody2D thisRigi;
	private Vector2 moveDirection;

	public bool isSpaced = false;
	public bool triedToMoveInSpace = false;

	public int gridX;
	public int gridY;

	//SNAPPING
	private bool isSnapping = false;
	private bool isSnapped = false;
	private float lerpTime = 0f;
	private Vector3 startPos;
	private Vector3 node;
	public float lerpSpeed;



	//TEMP
	public GameObject newGridHighlight;

	void Awake(){

		if (playerControl == null) {
		
			playerControl = this;
		
		} else {
		
			Destroy (this);
		
		}

	}
	// Use this for initialization
	void Start () {

		gridX = 22;
		gridY = 32;
		Vector2 newPos = new Vector2 (22f, 32f);
		transform.position = newPos;
		thisRigi = GetComponent<Rigidbody2D> ();
		playerRend = GetComponent<SpriteRenderer>();

		foreach(SpriteRenderer child in this.GetComponentsInChildren<SpriteRenderer>())
		{
			//Debug.Log(child.name + " " + child.tag);
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

	// Update is called once per frame
	void Update () {


		float moveHorizontal = Input.GetAxisRaw ("Horizontal");
		float moveVertical = Input.GetAxisRaw ("Vertical");
		int newGridY = gridY;
		int newGridX = gridX;

		if (moveVertical > 0 && moveHorizontal == 0f) //move up
			newGridY = gridY + 1;
		if (moveVertical < 0 && moveHorizontal == 0f) //move down
			newGridY = gridY - 1;
		if (moveHorizontal > 0 && moveVertical == 0f) //move right
			newGridX = gridX + 1;
		if (moveHorizontal < 0 && moveVertical == 0f) //move left
			newGridX = gridX - 1;
		if (moveVertical > 0 && moveHorizontal > 0) //Diag up + right
		{
			newGridY = gridY + 1;
			newGridX = gridX + 1; 
		}
		if (moveVertical > 0 && moveHorizontal < 0) //Diag up + left
		{
			newGridY = gridY + 1;
			newGridX = gridX - 1; 
		}
		if (moveVertical > 0 && moveHorizontal < 0) //Diag down + right
		{
			newGridY = gridY - 1;
			newGridX = gridX + 1; 
		}
		if (moveVertical > 0 && moveHorizontal < 0) //Diag down + left
		{
			newGridY = gridY - 1;
			newGridX = gridX - 1; 
		}

		moveDirection = new Vector2 (moveHorizontal, moveVertical);

		isMoving = (Mathf.Abs (moveDirection.x) + Mathf.Abs (moveDirection.y)) > 0f;
		GameManager.Direction direction = GameManager.Direction.Up;

		if (moveDirection.x > 0.5f && moveDirection.y == 0f) {
			//RIGHT
			playerRend.sprite = playerSheet [38];
			suitRend.sprite = suitSheet [238];
			beltRend.sprite = beltSheet [64];
			headRend.sprite = headSheet [223];
			feetRend.sprite = feetSheet [38];
			underwearRend.sprite = underwearSheet [54];
			uniformRend.sprite = uniformSheet [18];
			direction = GameManager.Direction.Right;

		}
		if (moveDirection.x < -0.5f && moveDirection.y == 0f) {
			//LEFT
			playerRend.sprite = playerSheet [39];
			suitRend.sprite = suitSheet [239];
			beltRend.sprite = beltSheet [65];
			headRend.sprite = headSheet [224];
			feetRend.sprite = feetSheet [39];
			underwearRend.sprite = underwearSheet [55];
			uniformRend.sprite = uniformSheet [19];
			direction = GameManager.Direction.Left;

		}
		if (moveDirection.y < 0f && moveDirection.x == 0f) {
			playerRend.sprite = playerSheet [36];
			suitRend.sprite = suitSheet [236];
			beltRend.sprite = beltSheet [62];
			headRend.sprite = headSheet [221];
			feetRend.sprite = feetSheet [36];
			underwearRend.sprite = underwearSheet [52];
			uniformRend.sprite = uniformSheet [16];
			direction = GameManager.Direction.Down;

		}
		if (moveDirection.y > 0f && moveDirection.x == 0f) {
			playerRend.sprite = playerSheet [37];
			suitRend.sprite = suitSheet [237];
			beltRend.sprite = beltSheet [63];
			headRend.sprite = headSheet [222];
			feetRend.sprite = feetSheet [37];
			underwearRend.sprite = underwearSheet [53];
			uniformRend.sprite = uniformSheet [17];
			direction = GameManager.Direction.Up;

		}
//		Debug.Log ("magnitude: " + thisRigi.velocity.magnitude);
//
//		if (isMoving) {
//			int calX = Mathf.Abs(gridX - newGridX);
//			int calY = Mathf.Abs(gridY - newGridY);
//			Debug.Log ("calXaNdY " + calX + " " + calY);
//			if (calX < 2 && calY < 2) {
//				gridX = newGridX;
//				gridY = newGridY;
//				var gridVector = gameManager.GetGridCoords (gridX, gridY);
//			
//				newGridHighlight.transform.position = gridVector;
//			}
//		
//		}

		if (isSnapping && !isSnapped) {
		
			lerpTime += Time.deltaTime;
			float t = lerpTime * lerpSpeed;

			transform.position = Vector3.Lerp (startPos, node, t);
		
			if (transform.position == node) {
				isSnapped = true;
				thisRigi.velocity = Vector3.zero;
			
			}
		}

	}

	void FixedUpdate(){


		if (isMoving) {
			if (!triedToMoveInSpace) {
				if (isSnapping) {
					isSnapping = false;
					isSnapped = false;
					lerpTime = 0f;
				}
				thisRigi.velocity = new Vector3 (moveDirection.x, moveDirection.y, 0).normalized * moveSpeed;
			}

			if (isSpaced && !triedToMoveInSpace) {
				triedToMoveInSpace = true;
				thisRigi.mass = 0f;
				thisRigi.drag = 0f;
				thisRigi.angularDrag = 0f;

			}
		} else {
			if (!isSpaced && !isSnapping) {
				startPos = transform.position;
				node = gameManager.GetClosestNode (transform.position);
				isSnapping = true;
			}
		
		}

	}
		
}

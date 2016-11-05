using UnityEngine;
using SS.GameLogic;

public class PlayerScript : MonoBehaviour {

    public GameObject playerCamera;
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

    private int gridX;
    private int gridY;

	private float timeBetweenFrames;

    // Use this for initialization
    void Start () {
        playerRend = GetComponent<SpriteRenderer>();

        gridX = 22;
        gridY = 32;

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
        //populate child sprites chef suit onto player
        //transform.position = gameManager.GetGridCoords(gridX, gridY);
		timeBetweenFrames = moveSpeed;
    }

    // Update is called once per frame
    void Update () {
		if (timeBetweenFrames < 0) {
			float moveHorizontal = Input.GetAxis("Horizontal");
			float moveVertical = Input.GetAxis("Vertical");
			int newGridY = gridY;
			int newGridX = gridX;
			if (moveVertical > 0 && moveHorizontal == 0f)
				newGridY = gridY + 1;
			if (moveVertical < 0 && moveHorizontal == 0f)
				newGridY = gridY - 1;
			if (moveHorizontal > 0 && moveVertical == 0f)
				newGridX = gridX + 1;
			if (moveHorizontal < 0 && moveVertical == 0f)
				newGridX = gridX - 1;

        //if (moveHorizontal > 0 || moveVertical > 0)
        //{
        //    Debug.Log("input:");
        //    Debug.Log(moveHorizontal + ", " + moveVertical);
        //    Debug.Log("grid xy:");
        //    Debug.Log(gridX + ", " + gridY);
        //    Debug.Log("grid vector:");
        //    Debug.Log(gameManager.GetGridCoords(gridX, gridY));
        //}


			Vector3 movement = new Vector3(moveHorizontal, moveVertical) * panSpeed;
			Vector2 moveDirection = new Vector2(movement.x, movement.y);
			Vector2 normalized = moveDirection.normalized;
			GameManager.Direction direction = GameManager.Direction.Up;
			if (normalized == Vector2.down) {
				playerRend.sprite = playerSheet[36];
				suitRend.sprite = suitSheet[236];
				beltRend.sprite = beltSheet[62];
				headRend.sprite = headSheet[221];
				feetRend.sprite = feetSheet[36];
				underwearRend.sprite = underwearSheet[52];
				uniformRend.sprite = uniformSheet[16];
				direction = GameManager.Direction.Down;
			}
			if (normalized == Vector2.up) {
				playerRend.sprite = playerSheet[37];
				suitRend.sprite = suitSheet[237];
				beltRend.sprite = beltSheet[63];
				headRend.sprite = headSheet[222];
				feetRend.sprite = feetSheet[37];
				underwearRend.sprite = underwearSheet[53];
				uniformRend.sprite = uniformSheet[17];
				direction = GameManager.Direction.Up;
			}
			if (normalized == Vector2.right) {
				playerRend.sprite = playerSheet[38];
				suitRend.sprite = suitSheet[238];
				beltRend.sprite = beltSheet[64];
				headRend.sprite = headSheet[223];
				feetRend.sprite = feetSheet[38];
				underwearRend.sprite = underwearSheet[54];
				uniformRend.sprite = uniformSheet[18];
				direction = GameManager.Direction.Right;
			}
			if (normalized == Vector2.left) {
				playerRend.sprite = playerSheet[39];
				suitRend.sprite = suitSheet[239];
				beltRend.sprite = beltSheet[65];
				headRend.sprite = headSheet[224];
				feetRend.sprite = feetSheet[39];
				underwearRend.sprite = underwearSheet[55];
				uniformRend.sprite = uniformSheet[19];
				direction = GameManager.Direction.Left;
			}

			if (gameManager.CheckPassable(gridX, gridY, direction)) {

				gridX = newGridX;
				gridY = newGridY;
				var gridVector = gameManager.GetGridCoords(gridX, gridY);
				transform.position = gridVector;
				playerCamera.transform.position = new Vector3(gridVector.x, gridVector.y, playerCamera.transform.position.z);

			}
			timeBetweenFrames = moveSpeed;
		} else {
			timeBetweenFrames = timeBetweenFrames - Time.deltaTime;
		}
    }
}

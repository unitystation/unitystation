using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
	public GameObject tile;
	public GameObject tileGrid;
	public int gridSizeX = 100;
	public int gridSizeY = 100;

	public GameObject playerCamera;
	public float panSpeed = 10.0f;

	private Sprite[] spaceSheet;
	private GameObject[,] grid;

	void InitTiles() {
		grid = new GameObject[gridSizeX, gridSizeY];
		//TODO find size of the sprite then use it to dynamically build the grid. Set to 32px right now
		//int tileWidth = (int)tile.GetComponent<SpriteRenderer>().bounds.size.x;
		//int tileHeight = (int)tile.GetComponent<SpriteRenderer>().bounds.size.y;
		TileManager tileManager;
		spaceSheet = Resources.LoadAll<Sprite>("turf/space"); //TODO replace this with AssetBundles for proper release

		for (int i = 0; i < gridSizeX; i++) {
			for (int j = 0; j < gridSizeY; j++) {
				grid[i, j] = Instantiate (tile);
				grid[i, j].transform.position = new Vector3 (i * 32, j * 32);
				grid[i, j].transform.SetParent (tileGrid.transform);

				tileManager = grid[i, j].GetComponent<TileManager>();
				int randomSpaceTile = (int)Random.Range(0, 101);
				tileManager.tileSprite = spaceSheet[randomSpaceTile];
			}
		}
	}

	// Use this for initialization
	void Start () {
		InitTiles();
	}
	
	// Update is called once per frame
	void Update () {
		float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");

		Vector3 movement = new Vector3 (moveHorizontal, moveVertical) * panSpeed;

		playerCamera.transform.position = playerCamera.transform.position + movement;
	}
}

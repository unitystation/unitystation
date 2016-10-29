using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
	public GameObject tile;
	public GameObject tileGrid;
	public Sprite spaceSheet; //TODO find out how to get the entire sheet rather than just the first sprite
	public int gridSizeX = 100;
	public int gridSizeY = 100;

	public GameObject playerCamera;
	public float panSpeed = 10.0f;

	private GameObject[,] grid;

	// Use this for initialization
	void Start () {
		grid = new GameObject[gridSizeX, gridSizeY];
		//TODO find size of the sprite then use it to dynamically build the grid. Set to 32px right now
		//int tileWidth = (int)tile.GetComponent<SpriteRenderer>().bounds.size.x;
		//int tileHeight = (int)tile.GetComponent<SpriteRenderer>().bounds.size.y;
		TileManager tileManager;
		for (int i = 0; i < gridSizeX; i++) {
			for (int j = 0; j < gridSizeY; j++) {
				grid[i, j] = Instantiate (tile);
				grid[i, j].transform.position = new Vector3 (i * 32, j * 32);
				grid[i, j].transform.SetParent (tileGrid.transform);
				tileManager = grid[i, j].GetComponent<TileManager>();
				tileManager.setSprite(spaceSheet); //TODO implement this
			}
		}
	}
	
	// Update is called once per frame
	void Update () {
		float moveHorizontal = Input.GetAxis ("Horizontal");
		float moveVertical = Input.GetAxis ("Vertical");

		Vector3 movement = new Vector3 (moveHorizontal, moveVertical) * panSpeed;

		playerCamera.transform.position = playerCamera.transform.position + movement;
	}
}

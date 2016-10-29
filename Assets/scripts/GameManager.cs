using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {
	public GameObject spaceTile;
	public GameObject tileGrid;
	public int gridSizeX = 100;
	public int gridSizeY = 100;

	public GameObject playerCamera;
	public float panSpeed = 10.0f;

	private GameObject[,] grid;

	// Use this for initialization
	void Start () {
		grid = new GameObject[gridSizeX, gridSizeY];
		int tileWidth = (int)spaceTile.GetComponent<SpriteRenderer>().bounds.size.x;
		int tileHeight = (int)spaceTile.GetComponent<SpriteRenderer>().bounds.size.y;
		for (int i = 0; i < gridSizeX; i++) {
			for (int j = 0; j < gridSizeY; j++) {
				//Debug.Log ("i: " + tileWidth + " j: " + tileHeight);
				grid [i, j] = Instantiate (spaceTile);
				grid [i, j].transform.position = new Vector3 (i * 32, j * 32);
				grid [i, j].transform.SetParent (tileGrid.transform);
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

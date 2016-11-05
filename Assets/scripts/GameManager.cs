using UnityEngine;
using System.Collections;

namespace SS.GameLogic {
	
	public class GameManager : MonoBehaviour {
		
		public GameObject tile;
		public GameObject tileGrid;
		public int gridSizeX = 100;
		public int gridSizeY = 100;

		public GameObject playerCamera;
		public float panSpeed = 10.0f;

		public enum Direction {Up, Left, Down, Right};
		public enum TileType {Space, Floor, Wall};

		private GameObject[,] grid;
		private TextAsset map;

		// Use this for initialization
		void Start () {
			InitTiles();
		}
		
		// Update is called once per frame
		void Update () {

			if (Input.GetKeyDown(KeyCode.O)) {
				map = Resources.Load<TextAsset>("maps/map.txt"); //TODO Get rid of ghetto map and resources.load
				LoadMap(map);
			}
		}

		private void InitTiles() {
			grid = new GameObject[gridSizeX, gridSizeY];
			TileManager tileManager;

			/*
			TODO find size of the sprite then use it to dynamically build the grid. Set to 32px right now
			int tileWidth = (int)spaceSheet[0].bounds.size.x;
			int tileHeight = (int)spaceSheet[0].bounds.size.y;

			Debug.Log(tileWidth + " " + tileHeight);
			*/
			for (int i = 0; i < gridSizeX; i++) {
				for (int j = 0; j < gridSizeY; j++) {
					grid[i, j] = Instantiate(tile);
					grid[i, j].transform.position = new Vector3(i * 32, j * 32);
					grid[i, j].transform.SetParent(tileGrid.transform);

					tileManager = grid[i, j].GetComponent<TileManager>();
					tileManager.gridX = i;
					tileManager.gridY = j;
					tileManager.gameManager = gameObject.GetComponent<GameManager>();
				}
			}
		}


		//TODO Implment reading the file and laying down floors/walls
		private void LoadMap(TextAsset map) {
		}

		public bool CheckPassable(int gridX, int gridY, Direction direction) {
			int newGridX = gridX;
			int newGridY = gridY;

			Direction newDirection = Direction.Up;

			switch (direction) {
			case Direction.Up:
				newGridY = newGridY + 1;
				newDirection = Direction.Down;
				break;
			case Direction.Right:
				newGridX = newGridX + 1;
				newDirection = Direction.Left;
				break;
			case Direction.Down:
				newGridY = newGridY - 1;
				newDirection = Direction.Up;
				break;
			case Direction.Left:
				newGridX = newGridX - 1;
				newDirection = Direction.Right;
				break;
			}

			if (newGridX > grid.GetLength(0) - 1 || newGridX < 0 || newGridY > grid.GetLength(1) - 1 || newGridY < 0) {
				Debug.Log("Attempting to move off map");
				return false;
			}

			return (
				grid[gridX, gridY].GetComponent<TileManager>().passable[(int)direction] && 
				grid[newGridX, newGridY].GetComponent<TileManager>().passable[(int)newDirection]
			);
		}
	}
}
using UnityEngine;
using System.Collections;
using SS.GameLogic;

public class TileManager : MonoBehaviour {
	public GameManager gameManager;
	public Sprite tileSprite; 

	public GameManager.TileType myTileType;
	public bool[] passable;

	public int gridX;
	public int gridY;

	private SpriteRenderer spriteRenderer;
	private GameObject displaySprite;

	// Use this for initialization
	void Start () {
		myTileType = GameManager.TileType.Space;
		passable = new bool[4]{true, true, true, true};
		displaySprite = transform.FindChild("DisplaySprite").gameObject;
		spriteRenderer = displaySprite.GetComponent<SpriteRenderer>();
		if (tileSprite) {
			spriteRenderer.sprite = tileSprite; 
		}
	}

	void OnMouseDown() {
		Debug.Log("X: " + gridX + " Y: " + gridY);
		Debug.Log(gameManager.CheckPassable(gridX, gridY, GameManager.Direction.Up));
	}

	//TODO long term this should change to a listener and simply changing the tileSprite will change the renderer sprite
	public void setSprite (Sprite sprite) {
		tileSprite = sprite;
		if (!gameObject.activeSelf) {
			Debug.Log("Tile not active yet!");
			return;
		}
		spriteRenderer.sprite = tileSprite; 
	}
}
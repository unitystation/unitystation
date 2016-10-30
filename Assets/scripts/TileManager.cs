using UnityEngine;
using System.Collections;

public class TileManager : MonoBehaviour {
	public Sprite tileSprite; 

	private SpriteRenderer spriteRenderer;
	private GameObject displaySprite;

	// Use this for initialization
	void Start () {
		displaySprite = transform.FindChild("DisplaySprite").gameObject;
		spriteRenderer = displaySprite.GetComponent<SpriteRenderer>();
		if (tileSprite) {
			spriteRenderer.sprite = tileSprite; 
		}
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
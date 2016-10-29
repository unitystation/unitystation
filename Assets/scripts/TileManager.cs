using UnityEngine;
using System.Collections;

public class TileManager : MonoBehaviour {
	public Sprite tileSprite; //TODO make private and set via method
	private SpriteRenderer spriteRenderer;

	// Use this for initialization
	void Start () {
		GameObject displaySprite = transform.FindChild("DisplaySprite").gameObject;
		spriteRenderer = displaySprite.GetComponent<SpriteRenderer>();
		spriteRenderer.sprite = tileSprite; //TODO remove
	}

	public void setSprite (Sprite sprite) {
		//TODO implement
	}
}
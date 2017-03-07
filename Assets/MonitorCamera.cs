using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sprites;

public class MonitorCamera : MonoBehaviour {

	private Sprite[] sprites;
	private SpriteRenderer thisSpriteRend;
	private int baseSprite = 2;
	void Start(){
		thisSpriteRend = GetComponentInChildren<SpriteRenderer>();
		sprites = SpriteManager.MonitorSprites["monitors"];
		int.TryParse(thisSpriteRend.sprite.name.Substring(9), out baseSprite);
		StartCoroutine(CameraMonitorAnim());
	}

	IEnumerator CameraMonitorAnim(){
		thisSpriteRend.sprite = sprites[baseSprite];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 8];
		yield return new WaitForSeconds(0.4f); //06
		thisSpriteRend.sprite = sprites[baseSprite + 16];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 24];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 32];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 40];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 48];
		yield return new WaitForSeconds(0.8f);
		thisSpriteRend.sprite = sprites[baseSprite + 40];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 32];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 24];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 16];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite + 8];
		yield return new WaitForSeconds(0.4f);
		thisSpriteRend.sprite = sprites[baseSprite];
		yield return new WaitForSeconds(0.4f);
		StartCoroutine(CameraMonitorAnim());
	}
}

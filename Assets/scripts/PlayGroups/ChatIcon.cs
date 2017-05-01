using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChatIcon : MonoBehaviour {

	public Sprite talkSprite;
	public Sprite questionSprite;
	public Sprite exlaimSprite;
	private SpriteRenderer spriteRend;

	private bool waitToTurnOff = false;

	// Use this for initialization
	void Start () {
		spriteRend = GetComponent<SpriteRenderer>();
		spriteRend.enabled = false;
	}
		
	public void TurnOnTalkIcon(){
		spriteRend.sprite = talkSprite;
		spriteRend.enabled = true;
		if (waitToTurnOff) {
			StopCoroutine(WaitToTurnOff());
		}
	}

	public void TurnOffTalkIcon(){
		if (!waitToTurnOff) {
			StartCoroutine(WaitToTurnOff());
		}
	}

	IEnumerator WaitToTurnOff(){
		yield return new WaitForSeconds(3f);
		spriteRend.enabled = false;
		waitToTurnOff = false;
	}
}

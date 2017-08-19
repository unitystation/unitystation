using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sprites;

public class BloodSplat : NetworkBehaviour {

	public SpriteRenderer spriteRend;
	private Sprite[] bloodSprites;

	[SyncVar(hook="SetSprite")]
	private int bloodSprite;

	public override void OnStartClient(){
		StartCoroutine(WaitForLoad());
		base.OnStartClient();
	}

	IEnumerator WaitForLoad(){
		yield return new WaitForSeconds(3f);
		SetSprite(bloodSprite);
	}
	//TODO streaky blood from bullet wounds, dragging blood drops etc
	[Server]
	public void SplatBlood(BloodSplatSize bloodSize){
		int spriteNum = 0;
		switch (bloodSize) {
			case BloodSplatSize.small:
				spriteNum = Random.Range(137, 139);
				break;
			case BloodSplatSize.medium:
				spriteNum = Random.Range(116, 120);
				break;
			case BloodSplatSize.large:
				spriteNum = Random.Range(105, 108);
				break;
		}
		bloodSprite = spriteNum;
	}
		
	void SetSprite(int spritenum){
		if (bloodSprites == null) {
			bloodSprites = SpriteManager.BloodSprites["blood"];
		}
		spriteRend.sprite = bloodSprites[spritenum];
		spriteRend.enabled = true;
	}
		
}

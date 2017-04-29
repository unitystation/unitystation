using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Sprites;

public class BloodSplat : NetworkBehaviour {

	private SpriteRenderer spriteRend;
	private Sprite[] bloodSprites;


	void Start(){
		bloodSprites = SpriteManager.BloodSprites["blood"];
		spriteRend = transform.Find("sprite").GetComponent<SpriteRenderer>();
		spriteRend.enabled = false;
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
		RpcSetSprite(spriteNum);
	}

	[ClientRpc]
	void RpcSetSprite(int spritenum){
		spriteRend.sprite = bloodSprites[spritenum];
		spriteRend.enabled = true;
	}
		
}

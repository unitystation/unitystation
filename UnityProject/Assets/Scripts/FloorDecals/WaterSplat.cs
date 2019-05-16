using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class WaterSplat : FloorDecal
{
	protected override void GetSprites()
	{
		Sprites = SpriteManager.WaterSprites["water"];
	}

	protected override void Splash()
	{
		StartCoroutine(DryUp());
	}

	private IEnumerator DryUp(){
		yield return new WaitForSeconds(15f);
		DisappearFromWorldServer();
	}
}
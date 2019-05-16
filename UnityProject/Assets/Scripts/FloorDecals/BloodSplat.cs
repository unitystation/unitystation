using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class BloodSplat : FloorDecal
{
	protected override void GetSprites()
	{
		Sprites = SpriteManager.BloodSprites["blood"];
	}
}
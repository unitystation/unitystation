using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;

public class AmbientTile : ObjectTrigger {

	private Color onColor = new Color32(105,105,105,255);
	private Color offColor = new Color32(0,0,0,255);
	private SpriteRenderer spriteRend;

	void Start(){
		spriteRend = GetComponent<SpriteRenderer>();
		spriteRend.color = onColor;
	}

	public override void Trigger(bool state)
	{
		if (spriteRend == null)
			return;
		
		spriteRend.color = state ? onColor : offColor;
	}
}

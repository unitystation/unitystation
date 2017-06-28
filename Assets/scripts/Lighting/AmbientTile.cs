using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputControl;

public class AmbientTile : ObjectTrigger {

	private Color onColor = new Color32(133,133,133,255);
	private Color offColor = new Color32(0,0,0,255);
	private SpriteRenderer spriteRend;

	void Start(){
		spriteRend = GetComponent<SpriteRenderer>();
	}

	public override void Trigger(bool state)
	{
		spriteRend.color = state ? onColor : offColor;
	}
}

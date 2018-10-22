using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackPackTrigger : PickUpTrigger
{
	public override void UI_Interact(GameObject originator, string hand){
		Debug.Log("UI INTERACT WITH BAG");
	}
}
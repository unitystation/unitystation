using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items;
using PlayGroup;

/// <summary>
/// Object behaviour controls all of the basic features of an object
/// like being able to hide the obj, being able to set on fire, throwing etc
/// </summary>
public class ObjectBehaviour : PushPull
{
	//Inspector is controlled by ObjectBehaviourEditor
	//Please expose any properties you need in there
	private PickUpTrigger pickUpTrigger;

	void Awake()
	{
		pickUpTrigger = GetComponent<PickUpTrigger>();
	}

	public override void OnMouseDown()
	{
		if(PlayerManager.LocalPlayerScript.IsInReach(transform)){
			//If this is an item with a pick up trigger and player is
			//not holding control, then check if it is being pulled
			//before adding to inventory
			if(!Input.GetKey(KeyCode.LeftControl) && pickUpTrigger !=
			   null && pulledBy != null){
				CancelPullBehaviour();
			}
		}

		base.OnMouseDown();
	}
}

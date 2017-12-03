using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Items;
using PlayGroup;
using Cupboards;

/// <summary>
/// Object behaviour controls all of the basic features of an object
/// like being able to hide the obj, being able to set on fire, throwing etc
/// </summary>
public class ObjectBehaviour : PushPull
{
	//Inspector is controlled by ObjectBehaviourEditor
	//Please expose any properties you need in there
	private PickUpTrigger pickUpTrigger;
	private PlayerScript playerScript;
	private ClosetPlayerHandler closetHandlerCache;

	protected override void Awake()
	{
		base.Awake();
		//Determines if it is an item 
		pickUpTrigger = GetComponent<PickUpTrigger>();
		//Determines if it is a player
		playerScript = GetComponent<PlayerScript>();
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

	public override void OnVisibilityChange(bool state){
		if(playerScript != null){
			if(PlayerManager.LocalPlayerScript == playerScript){
				//Local player, might be in a cupboard so add a cupboard handler. The handler will remove
				//itself if not needed
				//TODO turn the ClosetPlayerHandler into a more generic component to handle disposals bin,
				//coffins etc
				if (!state) {
					StartCoroutine(AddHiddenHandler());
				} else {
					if(closetHandlerCache){
						//Set the camera to follow the player again
						Camera2DFollow.followControl.target = transform;
						Camera2DFollow.followControl.damping = 0f;
						Destroy(closetHandlerCache);
					}
				}
			}
		}
	}

	IEnumerator AddHiddenHandler(){
		//wait for all the components to be disabled on the 
		//player before adding the handler
		yield return new WaitForEndOfFrame();
		closetHandlerCache = playerScript.gameObject.AddComponent<ClosetPlayerHandler>();
	}
}

using UnityEngine;
using System.Collections;
using PlayGroup;
using UI;

namespace Items
{
	
public class ItemManager : MonoBehaviour {

	public static ItemManager control;



	void Awake(){

	  if (control == null) {

	  control = this;

	  } else {

	  Destroy (this);

	  }
	}



	// Use this for initialization
	void Start () {

   
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}


		public void PickUpObject(GameObject itemObject){

            ItemAttributes attributes = itemObject.GetComponent<ItemAttributes>();
			PlayerSprites pSprites = PlayerManager.control.playerScript.playerSprites;

			//determine what hand is selected and if it is full
			if(PlayerManager.control.playerScript != null){

				if (UIManager.control.isRightHand) {
				
					//move the whole item into the hand
					UIManager.control.hands.rightSlot.AddItem(itemObject);
				
				
				} else if (!UIManager.control.isRightHand) {
				
					UIManager.control.hands.leftSlot.AddItem(itemObject);
				
				
				}



			}

            pSprites.PickedUpItem(6); //hard coded to kitchen knife data temporarily

            
			//TODO: communicate with playersprites and give it a reference to the items
			//TODO: carring sprites (lefthand, righthand etc). Remember to check if it is
			//TODO: right or left hand aswell.


		}
}
}

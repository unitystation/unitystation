using UnityEngine;
using System.Collections;
using SS.PlayGroup;
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

			//TODO determine what hand is selected and if it is full
			if(PlayerScript.playerControl != null){

				if (UIManager.control.isRightHand) {
				
					//TODO move the whole item into the hand
					UIManager.control.handSelector.rightSlot.AddItem(itemObject);
				
				
				} else if (!UIManager.control.isRightHand) {
				
					UIManager.control.handSelector.leftSlot.AddItem(itemObject);
				
				
				}



			}



			//TODO: communicate with playersprites and give it a reference to the items
			//TODO: carring sprites (lefthand, righthand etc). Remember to check if it is
			//TODO: right or left hand aswell.


		}
}
}

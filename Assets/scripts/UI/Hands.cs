using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI{
public class Hands : MonoBehaviour {
		//Handles left and right hand + selector

		// gObjs
		public GameObject selector;
		public GameObject leftHand;
		public GameObject rightHand;

		//item slots
		public UI_ItemSlot leftSlot;
		public UI_ItemSlot rightSlot;

		//components
		[HideInInspector]
		public HandActions handActions;

	// Use this for initialization
	void Start () {
			
			handActions = gameObject.AddComponent<HandActions> ();
	}
	

		/* 
		 * Button OnClick methods
		 */

		// whether selector should be on the right hand or the left
		public void SelectorState(bool isRight){
			//soundFX trial placement here
			if (UIManager.control != null) {
				PlayClick01 ();
				if (isRight) {
					
					UIManager.control.isRightHand = true;
					selector.transform.position = rightHand.transform.position;

				} else {
					UIManager.control.isRightHand = false;
					selector.transform.position = leftHand.transform.position;

				}
			}

		}

		//For swap button
		public void Swap(){
			PlayClick01 ();
	
			if (UIManager.control.isRightHand) {

				SelectorState (false);
			} else {
				SelectorState (true);
			
			}

		}
			

		//OnClick Methods
		public void Use(){
			PlayClick01 ();

		}

//		public void CheckAction(bool rightHand){
//		
//			handActions.Check (rightHand); //check if it should be used or given to other hand
//		}

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
				SoundManager.control.sounds[5].Play ();
			}

		}
}
}

using UnityEngine;
using System.Collections;

namespace SS.UI{
public class HandSelector : MonoBehaviour {
		//Handles left and right hand + selector

		// gObjs
		public GameObject selector;
		public GameObject leftHand;
		public GameObject rightHand;

		//bools
		private bool isRightHand = true;

	// Use this for initialization
	void Start () {
	

	}
	
		// whether selector should be on the right hand or the left
		public void SelectorState(bool isRight){
			//soundFX trial placement here
			ControlUI.control.click01SFX.Play ();
			if (isRight) {
				isRightHand = true;
				ControlUI.control.isRightHand = true;
				selector.transform.position = rightHand.transform.position;
			} else {
				isRightHand = false;
				ControlUI.control.isRightHand = false;
				selector.transform.position = leftHand.transform.position;
			}

		}

		//For swap button
		public void Swap(){

			if (isRightHand) {

				SelectorState (false);
			} else {
				SelectorState (true);
			
			}

		}
}
}

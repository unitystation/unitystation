using UnityEngine;
using UnityEngine.UI;
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

		//items
		public Image rightRend;
		public Image leftRend;

	// Use this for initialization
	void Start () {
	

	}
	



		/* 
		 * Button OnClick methods
		 */

		// whether selector should be on the right hand or the left
		public void SelectorState(bool isRight){
			//soundFX trial placement here
			if (ControlUI.control != null) {
				PlayClick01 ();
				if (isRight) {
					Debug.Log ("RightHand Button");
					isRightHand = true;
					ControlUI.control.isRightHand = true;
					selector.transform.position = rightHand.transform.position;
				} else {
					Debug.Log ("LeftHand Button");
					isRightHand = false;
					ControlUI.control.isRightHand = false;
					selector.transform.position = leftHand.transform.position;
				}
			}

		}

		//For swap button
		public void Swap(){
			PlayClick01 ();
			Debug.Log ("Swap Button");
			if (isRightHand) {

				SelectorState (false);
			} else {
				SelectorState (true);
			
			}

		}

		//OnClick Methods

		public void Use(){
			PlayClick01 ();
			Debug.Log ("Use Button");

		}

		//SoundFX

		void PlayClick01(){

			if (ControlUI.control.click01SFX != null) {
				ControlUI.control.click01SFX.Play ();
			}

		}
}
}

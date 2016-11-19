using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI{
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
			if (UIManager.control != null) {
				PlayClick01 ();
				if (isRight) {
					Debug.Log ("RightHand Button");
					isRightHand = true;
					UIManager.control.isRightHand = true;
					selector.transform.position = rightHand.transform.position;
				} else {
					Debug.Log ("LeftHand Button");
					isRightHand = false;
					UIManager.control.isRightHand = false;
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

			if (UIManager.control.click01SFX != null) {
				UIManager.control.click01SFX.Play ();
			}

		}
}
}

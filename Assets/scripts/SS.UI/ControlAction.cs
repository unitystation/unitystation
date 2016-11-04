using UnityEngine;
using UnityEngine.UI;
using System.Collections;


namespace SS.UI{


public class ControlAction : MonoBehaviour {


		public Sprite[] throwSprites;
		public Image throwImage;




		void Start(){
			ControlUI.control.isThrow = false;

		}

		/* 
		 * Button OnClick methods
		 */


		public void Resist(){
			PlayClick01 ();
			Debug.Log ("Resist Button");


		}

		public void  Drop(){
			PlayClick01 ();
			Debug.Log ("Drop Button");



		}

		public void Throw(){
			PlayClick01 ();
			Debug.Log ("Throw Button");

			if (!ControlUI.control.isThrow) {
			
				ControlUI.control.isThrow = true;
				throwImage.sprite = throwSprites [1];
			
			} else {
			
				ControlUI.control.isThrow = false;
				throwImage.sprite = throwSprites [0];
			
			
			}

		}

		//SoundFX

		void PlayClick01(){

			if (ControlUI.control.click01SFX != null) {
				ControlUI.control.click01SFX.Play ();
			}

		}
}
}
using UnityEngine;
using UnityEngine.UI;
using System.Collections;


namespace SS.UI{


public class ControlAction : MonoBehaviour {


		public Sprite[] throwSprites;
		public Image throwImage;

		public bool isThrow{ get; set; }


		void Start(){
			isThrow = false;

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

			if (!isThrow) {
			
				isThrow = true;
				throwImage.sprite = throwSprites [1];
			
			} else {
			
				isThrow = false;
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
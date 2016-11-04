using UnityEngine;
using System.Collections;

namespace SS.UI{
public class ControlBottomUI : MonoBehaviour {


		/* 
		 * Button OnClick methods
		 */

		public void SuitStorage(){
			PlayClick01 ();
			Debug.Log ("SuitStorage Button");

		}

		public void ID(){
			PlayClick01 ();
			Debug.Log ("ID Button");

		}

		public void Belt(){
			PlayClick01 ();
			Debug.Log ("Belt Button");

		}

		public void Bag(){
			PlayClick01 ();
			//FIXME remove after adding the bag control class
			// that will also control the inventory
			Debug.Log ("Bag");

		}

		public void Storage01(){
			PlayClick01 ();
			Debug.Log ("Storage01 Button");

		}

		public void Storage02(){
			PlayClick01 ();
			Debug.Log ("Storage02 Button");

		}
			

		//SoundFX

		void PlayClick01(){

			if (ControlUI.control.click01SFX != null) {
				ControlUI.control.click01SFX.Play ();
			}

		}
}
}
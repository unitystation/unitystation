using UnityEngine;
using System.Collections;

namespace UI{
public class ControlBottomUI : MonoBehaviour {


		public UI_ItemSlot storage01Slot;
		public UI_ItemSlot storage02Slot;

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
            storage01Slot.TryToSwapItem(UIManager.control.hands.currentSlot);
		}

		public void Storage02(){
			PlayClick01 ();
            storage02Slot.TryToSwapItem(UIManager.control.hands.currentSlot);
		}
			

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
				SoundManager.control.sounds["Click01"].Play ();
			}

		}
}
}
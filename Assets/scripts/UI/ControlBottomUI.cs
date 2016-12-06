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
			if (!storage01Slot.isFull) {
				if (UIManager.control.isRightHand && UIManager.control.hands.rightSlot.isFull) {
			
					storage01Slot.TryToAddItem (UIManager.control.hands.rightSlot.inHandItem);
					UIManager.control.hands.rightSlot.inHandItem = null;
			
				} else if (!UIManager.control.isRightHand && UIManager.control.hands.leftSlot.isFull) {

					storage01Slot.TryToAddItem (UIManager.control.hands.leftSlot.inHandItem);
					UIManager.control.hands.leftSlot.inHandItem = null;

				}
			}

		}

		public void Storage02(){
			PlayClick01 ();

			if (!storage02Slot.isFull) {
				if (UIManager.control.isRightHand && UIManager.control.hands.rightSlot.isFull) {

					storage02Slot.TryToAddItem (UIManager.control.hands.rightSlot.inHandItem);
					UIManager.control.hands.rightSlot.inHandItem = null;

				} else if (!UIManager.control.isRightHand && UIManager.control.hands.leftSlot.isFull) {

					storage02Slot.TryToAddItem (UIManager.control.hands.leftSlot.inHandItem);
					UIManager.control.hands.leftSlot.inHandItem = null;

				}
			}

		}
			

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
				SoundManager.control.sounds["Click01"].Play ();
			}

		}
}
}
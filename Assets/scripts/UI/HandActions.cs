using UnityEngine;
using System.Collections;

namespace UI{
public class HandActions : MonoBehaviour {

		private Hands hands;


		void Start(){
			hands = GetComponent<Hands> ();

		}



		public void ActionLogic(SlotType slotType){

			if (UIManager.control.isRightHand && slotType == SlotType.leftHand && !hands.rightSlot.isFull) {
				//Taking item from left hand and giving to the right if it is empty

				//Just pass a sprite for time being
				SwapItem ();
			} else if (!UIManager.control.isRightHand && slotType == SlotType.rightHand && !hands.leftSlot.isFull) {
				//Taking item from right hand and giving to the left if it is empty
				SwapItem ();
			} else {

				if (UIManager.control.isRightHand && !UIManager.control.hands.rightSlot.isFull) {
				
					if (slotType == SlotType.storage01) {
						UIManager.control.hands.rightSlot.AddItem (UIManager.control.bottomControl.storage01Slot.inHandItem);
						UIManager.control.bottomControl.storage01Slot.inHandItem = null;
					
					}
					if (slotType == SlotType.storage02) {
						UIManager.control.hands.rightSlot.AddItem (UIManager.control.bottomControl.storage02Slot.inHandItem);
						UIManager.control.bottomControl.storage02Slot.inHandItem = null;

					}

				
				} else if (!UIManager.control.isRightHand && !UIManager.control.hands.leftSlot.isFull) {

					if (slotType == SlotType.storage01) {
						UIManager.control.hands.leftSlot.AddItem (UIManager.control.bottomControl.storage01Slot.inHandItem);
						UIManager.control.bottomControl.storage01Slot.inHandItem = null;

					}
					if (slotType == SlotType.storage02) {
						UIManager.control.hands.leftSlot.AddItem (UIManager.control.bottomControl.storage02Slot.inHandItem);
						UIManager.control.bottomControl.storage02Slot.inHandItem = null;

					}


				}


			}



		}



		void SwapItem(){


			if (UIManager.control.isRightHand) {
				hands.rightSlot.AddItem(hands.leftSlot.inHandItem);
				hands.leftSlot.inHandItem = null;
			

			
			} else {
				hands.leftSlot.AddItem(hands.rightSlot.inHandItem);
				hands.rightSlot.inHandItem = null;

			
			}


		}
}
}

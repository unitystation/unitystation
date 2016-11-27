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
				SwapItem();
			} 

			if (!UIManager.control.isRightHand && slotType == SlotType.rightHand && !hands.leftSlot.isFull) {
				//Taking item from right hand and giving to the left if it is empty
				SwapItem();
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

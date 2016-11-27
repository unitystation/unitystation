using UnityEngine;
using System.Collections;

namespace UI{
public class HandActions : MonoBehaviour {

		private HandSelector handSelector;


		void Start(){
			handSelector = GetComponent<HandSelector> ();

		}

		//TODO change from passing around a sprite to an actual item class

//		public void Check(bool rightHand){
//
//			if (UIManager.control.isRightHand && !rightHand && !handSelector.rightSlot.isFull) {
//					//Taking item from left hand and giving to the right if it is empty
//
//				//Just pass a sprite for time being
//				SwapItem();
//			} 
//
//			if (!UIManager.control.isRightHand && rightHand && !handSelector.leftSlot.isFull) {
//				//Taking item from right hand and giving to the left if it is empty
//				SwapItem();
//			}
//
//		}

		//TODO: pass actual items not the temp sprite

//		void SwapItem(){
//
//
//			if (UIManager.control.isRightHand) {
//				handSelector.rightSlot.AddItem(handSelector.leftSlot.GetItem ());
//			
//
//			
//			} else {
//				handSelector.leftSlot.AddItem(handSelector.rightSlot.GetItem ());
//
//			
//			}
//
//
//		}
}
}

using UnityEngine;
using System.Collections;

namespace UI {
    public class HandActions: MonoBehaviour {

        private Hands hands;
        
        void Start() {
            hands = GetComponent<Hands>();
        }

        public void ActionLogic(SlotType slotType) {

            if(UIManager.control.isRightHand && slotType == SlotType.leftHand) {
                //Taking item from left hand and giving to the right if it is empty
                SwapItem();
            } else if(!UIManager.control.isRightHand && slotType == SlotType.rightHand) {
                //Taking item from right hand and giving to the left if it is empty
                SwapItem();
            } else {
                if(UIManager.control.isRightHand && !UIManager.control.hands.rightSlot.isFull) {

                    if(slotType == SlotType.storage01) {
                        UIManager.control.hands.rightSlot.TryToAddItem(UIManager.control.bottomControl.storage01Slot.inHandItem);
                        UIManager.control.bottomControl.storage01Slot.inHandItem = null;
                    }
                    if(slotType == SlotType.storage02) {
                        UIManager.control.hands.rightSlot.TryToAddItem(UIManager.control.bottomControl.storage02Slot.inHandItem);
                        UIManager.control.bottomControl.storage02Slot.inHandItem = null;
                    }

                } else if(!UIManager.control.isRightHand && !UIManager.control.hands.leftSlot.isFull) {

                    if(slotType == SlotType.storage01) {
                        UIManager.control.hands.leftSlot.TryToAddItem(UIManager.control.bottomControl.storage01Slot.inHandItem);
                        UIManager.control.bottomControl.storage01Slot.inHandItem = null;

                    }
                    if(slotType == SlotType.storage02) {
                        UIManager.control.hands.leftSlot.TryToAddItem(UIManager.control.bottomControl.storage02Slot.inHandItem);
                        UIManager.control.bottomControl.storage02Slot.inHandItem = null;

                    }
                }
            }
        }

        void SwapItem() {
            if(UIManager.control.isRightHand) {
                if(hands.rightSlot.TryToAddItem(hands.leftSlot.inHandItem))
                    hands.leftSlot.inHandItem = null;
            } else {
                if(hands.leftSlot.TryToAddItem(hands.rightSlot.inHandItem))
                    hands.rightSlot.inHandItem = null;
            }
        }
    }
}

using UnityEngine;
using System.Collections;
using PlayGroup;

namespace UI {
    public class HandActions: MonoBehaviour {

        private Hands hands;

        void Start() {
            hands = UIManager.control.hands;
        }

        public void SwapItem(SlotType slotType) {
            switch(slotType) {
                case SlotType.rightHand:
                    if(!UIManager.control.isRightHand) 
                        hands.leftSlot.TryToSwapItem(hands.rightSlot);
                    break;
                case SlotType.leftHand:
                    if(UIManager.control.isRightHand)
                        hands.rightSlot.TryToSwapItem(hands.leftSlot);
                    break;
                case SlotType.storage01:
                    hands.currentSlot.TryToSwapItem(UIManager.control.bottomControl.storage01Slot);
                    break;
                case SlotType.storage02:
                    hands.currentSlot.TryToSwapItem(UIManager.control.bottomControl.storage02Slot);
                    break;
            }
        }
    }
}

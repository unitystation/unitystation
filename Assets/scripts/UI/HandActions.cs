using UnityEngine;
using System.Collections;
using PlayGroup;

namespace UI {
    public class HandActions: MonoBehaviour {

        private Hands hands;

        void Start() {
            hands = UIManager.control.hands;
        }

        public void SwapItem(UI_ItemSlot itemSlot) {
            if(hands.currentSlot != itemSlot) {
                hands.currentSlot.TryToSwapItem(itemSlot);
            }
        }
    }
}

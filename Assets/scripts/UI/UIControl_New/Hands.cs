using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace UI {
    public class Hands: MonoBehaviour {
        public UI_ItemSlot CurrentSlot { get; private set; }
        public bool IsRight { get; private set; }

        public UI_ItemSlot RightSlot;
        public UI_ItemSlot LeftSlot;
        public Transform selector;

        void Start() {
            CurrentSlot = RightSlot;
            IsRight = true;
        }

        public void Swap() {
            Swap(!IsRight);
        }

        public void Swap(bool right) {
            if(right) {
                CurrentSlot = RightSlot;
            }else {
                CurrentSlot = LeftSlot;
            }

            IsRight = right;
            selector.position = CurrentSlot.transform.position;
        }

        public void SwapItem(UI_ItemSlot itemSlot) {
            if(CurrentSlot != itemSlot) {
                CurrentSlot.TryToSwapItem(itemSlot);
            }
        }
    }
}
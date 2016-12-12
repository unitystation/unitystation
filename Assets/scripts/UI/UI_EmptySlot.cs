using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace UI {
    public class UI_EmptySlot: MonoBehaviour, IPointerClickHandler {

        private UI_ItemSlot itemSlot;
        
        void Start() {
            itemSlot = GetComponentInChildren<UI_ItemSlot>();
        }

        public void OnPointerClick(PointerEventData eventData) {
            SoundManager.control.Play("Click01");
            itemSlot.TryToSwapItem(UIManager.control.hands.currentSlot);
        }
    }
}

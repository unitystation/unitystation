using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


namespace UI
{
    public class UI_ItemSwap : MonoBehaviour, IPointerClickHandler
    {

        private UI_ItemSlot itemSlot;

        void Start()
        {
            itemSlot = GetComponentInChildren<UI_ItemSlot>();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            SoundManager.Play("Click01");
            UIManager.Hands.SwapItem(itemSlot);
        }
    }
}

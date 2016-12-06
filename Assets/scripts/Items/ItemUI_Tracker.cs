using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UI;

namespace Items {

    public class ItemUI_Tracker: MonoBehaviour, IPointerClickHandler {

        private SpriteRenderer thisSpriteRend;
        private RectTransform thisRect;
        private Button thisButton;
        private Image thisImg;

        //Scale cache
        private Vector2 IGscale;


        public SlotType slotType { get; set; }

        // Use this for initialization
        void Start() {

            IGscale = this.gameObject.transform.localScale; //STORE THE CURRENT SCALE TO RESET WHEN RETURNING BACK INTO THE GAME
            thisSpriteRend = GetComponentInChildren<SpriteRenderer>();
            thisRect = gameObject.AddComponent<RectTransform>();
            thisImg = gameObject.AddComponent<Image>();
            thisButton = gameObject.AddComponent<Button>();


            thisImg.sprite = thisSpriteRend.sprite;
            thisRect.sizeDelta = new Vector2(32f, 32f);

        }

        public void OnPointerClick(PointerEventData eventData) {

            Debug.Log("Clicked on item " + gameObject.name);
            UIManager.control.hands.actions.SwapItem(slotType);

        }




    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using Items;
using PlayGroup;

namespace UI {

    public enum SlotType {
        None,
        rightHand,
        leftHand,
        storage01,
        storage02

    }

    public class UI_ItemSlot: MonoBehaviour, IPointerClickHandler {

        public SlotType slotType;

        public bool isFull {
            get {
                if(currentItem == null) {
                    return false;
                } else {
                    return true;
                }
            }
        }

        public GameObject Item {
            get { return currentItem; }
        }


        private GameObject currentItem;
        private PlayerSprites playerSprites;

        private Image image;

        void Start() {
            playerSprites = PlayerManager.control.playerScript.playerSprites;
            image = GetComponent<Image>();
            image.enabled = false;
        }

        public bool TryToAddItem(GameObject item) {
            if(!isFull && item != null) {

                if(slotType == SlotType.storage01 || slotType == SlotType.storage02) {
                    var attributes = item.GetComponent<ItemAttributes>();

                    if(attributes.size != Size.Small) {
                        Debug.Log("Item is too big!");
                        return false;
                    }
                }

                image.sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
                image.enabled = true;

                currentItem = item;
                item.transform.position = transform.position;
                item.transform.parent = this.gameObject.transform;

                playerSprites.PickedUpItem(item);


                return true;
            }
            return false;
        }

        /// <summary>
        /// Tries to add item from another slot
        /// </summary>
        /// <param name="otherSlot"></param>
        /// <returns></returns>
        public bool TryToSwapItem(UI_ItemSlot otherSlot) {
            if(!isFull && TryToAddItem(otherSlot.currentItem)) {
                var item = otherSlot.RemoveItem();

                if(slotType == SlotType.leftHand || slotType == SlotType.rightHand)
                    playerSprites.PickedUpItem(item);
                return true;
            }
            return false;
        }

        /// <summary>
        /// removes item from slot
        /// </summary>
        /// <returns></returns>
        public GameObject RemoveItem() {
            if(isFull) {
                if(slotType == SlotType.leftHand || slotType == SlotType.rightHand)
                    playerSprites.RemoveItemFromHand(slotType == SlotType.rightHand);

                var item = currentItem;
                currentItem = null;

                image.sprite = null;
                image.enabled = false;
                return item;
            }
            return null;
        }
        
        public void OnPointerClick(PointerEventData eventData) {

            Debug.Log("Clicked on item " + currentItem.name);
            UIManager.control.hands.actions.SwapItem(slotType);

        }
    }
}
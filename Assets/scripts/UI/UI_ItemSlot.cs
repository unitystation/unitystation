using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PlayGroup;
using System.Collections.Generic;

namespace UI {

    public enum SlotType {
        Other,
        RightHand,
        LeftHand
    }

    public class UI_ItemSlot: MonoBehaviour, IPointerClickHandler {
        
        public SlotType slotType;
        public bool allowAllItems;
        public List<ItemType> allowedItemTypes;
        public ItemSize maxItemSize;

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

        private UI_ClothingTrigger clothingTrigger;

        void Start() {

            playerSprites = PlayerManager.control.playerScript.playerSprites;
            image = GetComponent<Image>();
            image.enabled = false;

            clothingTrigger = GetComponent<UI_ClothingTrigger>();
        }

        public bool TryToAddItem(GameObject item) {
            if(!isFull && item != null) {
                var attributes = item.GetComponent<ItemAttributes>();

                if(!allowAllItems && !allowedItemTypes.Contains(attributes.type)) {
                    return false;
                }

                if(allowAllItems && maxItemSize != ItemSize.Large && 
                    (maxItemSize != ItemSize.Medium || attributes.size == ItemSize.Large) && maxItemSize != attributes.size) {
                    Debug.Log("Item is too big!");
                    return false;
                }

                image.sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
                image.enabled = true;

                currentItem = item;

                if(clothingTrigger != null) {
                    clothingTrigger.UpdateClothing(item);
                }

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

                if(slotType == SlotType.LeftHand || slotType == SlotType.RightHand)
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
                if(slotType == SlotType.LeftHand || slotType == SlotType.RightHand) { 
                    playerSprites.RemoveItemFromHand(slotType == SlotType.RightHand);
                } else {
                    clothingTrigger.RemoveClothing();
                }

                var item = currentItem;
                currentItem = null;

                image.sprite = null;
                image.enabled = false;
                return item;
            }
            return null;
        }


        /// <summary>
        /// Empty slot and destroy item
        /// </summary>
        public void Reset() {
            if(isFull) {
                Destroy(RemoveItem());
            }
        }
        
        public void OnPointerClick(PointerEventData eventData) {
            SoundManager.control.Play("Click01");
            Debug.Log("Clicked on item " + currentItem.name);
            UIManager.control.hands.SwapItem(this);
        }
    }
}
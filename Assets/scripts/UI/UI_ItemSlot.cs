using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using PlayGroup;
using System.Collections.Generic;
using System.Collections;

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
        public string clothingName = null;
        
        public bool hasClothing {
            get {
                return clothingName != null && string.Empty != clothingName;
            }
        }

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
        private Equipment equipment;
        private Image image;
        
        void Awake() {
            image = GetComponent<Image>();
            image.enabled = false;
        }

        void LateUpdate() {
            if(playerSprites == null && PlayerManager.control.playerScript != null) //Wait for player to be spawned before assigning ref
            {
                playerSprites = PlayerManager.control.playerScript.playerSprites;
                equipment = PlayerManager.control.Equipment;
            }
        }

        public bool TryToAddItem(GameObject item) {
            if(!isFull && item != null) {

                if(CheckConditions(item)) {
                    if(hasClothing) {
                        equipment.UpdateClothing(transform.parent.name, item);
                    }

                    SetItem(item);

                    return true;
                }
            }
            return false;
        }

        public void SetItem(GameObject item) {
            image.sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
            image.enabled = true;

            currentItem = item;
            item.transform.position = transform.position;
            item.transform.parent = transform;
        }

        /// <summary>
        /// Tries to add item from another slot
        /// </summary>
        /// <param name="otherSlot"></param>
        /// <returns></returns>
        public bool TryToSwapItem(UI_ItemSlot otherSlot) {
            if(!isFull && TryToAddItem(otherSlot.currentItem)) {
                var item = otherSlot.RemoveItem();
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
                } else if(hasClothing) {
                    equipment.ClearClothing(transform.parent.name);
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
        /// Empties slot and destroy item
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


        private bool CheckConditions(GameObject item) {
            var attributes = item.GetComponent<ItemAttributes>();
            if(!allowAllItems && !allowedItemTypes.Contains(attributes.type)) {
                return false;
            }

            if(allowAllItems && maxItemSize != ItemSize.Large &&
                    (maxItemSize != ItemSize.Medium || attributes.size == ItemSize.Large) && maxItemSize != attributes.size) {
                Debug.Log("Item is too big!");
                return false;
            }

            return true;
        }
    }
}
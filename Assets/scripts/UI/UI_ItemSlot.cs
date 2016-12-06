using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Items;
using PlayGroup;

namespace UI {

    public enum SlotType {
        rightHand,
        leftHand,
        storage01,
        storage02

    }

    public class UI_ItemSlot: MonoBehaviour {
        
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



        void Start() {
            playerSprites = PlayerManager.control.playerScript.playerSprites;
        }

        public bool TryToAddItem(GameObject item) {
            if(!isFull && item != null) {
                ItemUI_Tracker itemTracker = item.GetComponent<ItemUI_Tracker>();
                if(itemTracker == null) {
                    itemTracker = item.AddComponent<ItemUI_Tracker>();
                }
                itemTracker.slotType = slotType;

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
            if(slotType == SlotType.leftHand || slotType == SlotType.rightHand)
                playerSprites.RemoveItemFromHand(slotType == SlotType.rightHand);

            var item = currentItem;
            currentItem = null;
            return item;
        }
    }
}
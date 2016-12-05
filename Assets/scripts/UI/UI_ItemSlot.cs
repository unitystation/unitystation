using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Items;

namespace UI {

    public enum SlotType {
        rightHand,
        leftHand,
        storage01,
        storage02

    }

    public class UI_ItemSlot: MonoBehaviour {

        public bool isFull {
            get {
                if(inHandItem == null) {
                    return false;
                } else {
                    return true;
                }
            }
        }

        public GameObject inHandItem;
        public SlotType thisSlot;

        void Start() {
        }

        public bool TryToAddItem(GameObject itemObj) {
            if(!isFull) {
                ItemUI_Tracker itemTracker = itemObj.GetComponent<ItemUI_Tracker>();
                if(itemTracker == null) {
                    itemTracker = itemObj.AddComponent<ItemUI_Tracker>();
                }
                itemTracker.slotType = thisSlot;

                inHandItem = itemObj;
                itemObj.transform.position = transform.position;
                itemObj.transform.parent = this.gameObject.transform;

                return true;
            }
            return false;
        }
    }
}
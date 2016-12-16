using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Events;
using UnityEngine.Events;

namespace UI {

    public class UI_ItemSlot: MonoBehaviour {

        public string eventName;
        public bool allowAllItems;
        public List<ItemType> allowedItemTypes;
        public ItemSize maxItemSize;

        private Image image;

        public GameObject Item { get; private set; }
        public bool IsFull { get; private set; }

        void Awake() {
            IsFull = false;
            image = GetComponent<Image>();
            image.enabled = false;

            EventManager.AddUIListener(eventName, new UnityAction<GameObject>(x => TrySetItem(x)));
        }

        public void SetItem(GameObject item) {
            image.sprite = item.GetComponentInChildren<SpriteRenderer>().sprite;
            image.enabled = true;

            Item = item;
            item.transform.position = transform.position;
            item.transform.parent = transform;

            IsFull = true;
            EventManager.TriggerUIEvent(eventName, item);
        }

        public bool TrySetItem(GameObject item) {
            if(!IsFull && item != null && CheckItemFit(item)) {
                SetItem(item);

                return true;
            }
            return false;
        }

        /// <summary>
        /// removes item from slot
        /// </summary>
        /// <returns></returns>
        public GameObject Clear() {
            var item = Item;
            Item = null;

            EventManager.TriggerUIEvent(eventName, null);

            image.sprite = null;
            image.enabled = false;

            IsFull = false;

            return item;
        }

        public void Reset() {
            if(IsFull) {
                Destroy(Clear());
            }
        }

        private bool CheckItemFit(GameObject item) {
            var attributes = item.GetComponent<ItemAttributes>();
            if(!allowAllItems) {
                if(!allowedItemTypes.Contains(attributes.type)) {
                    return false;
                }
            }else if(maxItemSize != ItemSize.Large && (maxItemSize != ItemSize.Medium || attributes.size == ItemSize.Large) && maxItemSize != attributes.size) {
                Debug.Log("Item is too big!");
                return false;
            }

            return true;
        }
    }
}
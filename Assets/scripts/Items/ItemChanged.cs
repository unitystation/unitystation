using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Events;
using UnityEngine.Events;

namespace PlayGroup {
    public class ItemChanged: MonoBehaviour {

        public string eventName;

        private ClothingItem clothingItem;

        void Start() {
            clothingItem = GetComponent<ClothingItem>();

            EventManager.AddUIListener(eventName, new UnityAction<GameObject>(OnChanged));
        }

        void OnChanged(GameObject item) {
            if(item) {
                clothingItem.UpdateItem(item);
            }else {
                clothingItem.Clear();
            }
        }
    }
}

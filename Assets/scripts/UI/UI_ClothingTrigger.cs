using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI {

    public class UI_ClothingTrigger: MonoBehaviour {

        public string clothingName;

        private ClothingItem clothingItem;

        void Start() {
            clothingItem = PlayerManager.control.LocalPlayer.transform.FindChild(clothingName).GetComponent<ClothingItem>();

        }

        void Update() {

        }

        public void UpdateClothing(GameObject item) {
            clothingItem.UpdateReference(item.GetComponent<ItemAttributes>());
        }

        public void RemoveClothing() {
            clothingItem.Clear();
        }
    }
}

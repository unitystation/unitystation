using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI {

    public class LoadOutManager: MonoBehaviour {
        public int faceReference = -1;
        public int bodyReference = 0;
        public int underwearReference = -1;

        public GameObject suitPrefab;
        public GameObject beltPrefab;
        public GameObject shoesPrefab;
        public GameObject headPrefab;
        public GameObject maskPrefab;
        public GameObject uniformPrefab;
        public GameObject neckPrefab;
        public GameObject earPrefab;
        public GameObject glassesPrefab;

        public GameObject leftHandPrefab;
        public GameObject rightHandPrefab;

        public GameObject idPrefab;
        public GameObject bagPrefab;
        public GameObject storage01Prefab;
        public GameObject storage02Prefab;
        public GameObject suitStoragePrefab;

        private bool finished = false;

        private Dictionary<string, UI_ItemSlot> itemSlots = new Dictionary<string, UI_ItemSlot>();

        void Start() {
            foreach(var itemSlot in GetComponentsInChildren(typeof(UI_ItemSlot), true)) {
                var name = itemSlot.transform.parent.name;

                itemSlots.Add(name, (UI_ItemSlot) itemSlot);

                SetPlayerLoadOuts();
            }
        }
        
        void SetPlayerLoadOuts() {
            var playerTransform = PlayerManager.control.LocalPlayer.transform;
            playerTransform.FindChild("face").GetComponent<ClothingItem>().Reference = faceReference;
            playerTransform.FindChild("body").GetComponent<ClothingItem>().Reference = bodyReference;
            playerTransform.FindChild("underwear").GetComponent<ClothingItem>().Reference = underwearReference;

            SetItemSlot("Suit", suitPrefab);
            SetItemSlot("Belt", beltPrefab);
            SetItemSlot("Shoes", shoesPrefab);
            SetItemSlot("Hat", headPrefab);
            SetItemSlot("Mask", maskPrefab);
            SetItemSlot("Uniform", uniformPrefab);
            SetItemSlot("Neck", neckPrefab);
            SetItemSlot("Ear", earPrefab);
            SetItemSlot("Glasses", glassesPrefab);

            SetItemSlot("ID", idPrefab);
            SetItemSlot("Bag", bagPrefab);
            SetItemSlot("RightHand", rightHandPrefab);
            SetItemSlot("LeftHand", leftHandPrefab);
            SetItemSlot("Storage01", storage01Prefab);
            SetItemSlot("Storage02", storage02Prefab);
        }

        void SetItemSlot(string slotName, GameObject prefab) {
            if(itemSlots.ContainsKey(slotName) && prefab != null) {
                var item = Instantiate(prefab);
                itemSlots[slotName].TryToAddItem(item);
            }
        }
    }
}
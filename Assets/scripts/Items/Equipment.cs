using Events;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI {

    public class Equipment: MonoBehaviour {
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
        public GameObject glovesPrefab;

        public GameObject leftHandPrefab;
        public GameObject rightHandPrefab;

        public GameObject idPrefab;
        public GameObject bagPrefab;
        public GameObject storage01Prefab;
        public GameObject storage02Prefab;
        public GameObject suitStoragePrefab;

        private ClothingItem face;
        private ClothingItem body;
        private ClothingItem underwear;

        private Dictionary<string, UI_ItemSlot> itemSlots = new Dictionary<string, UI_ItemSlot>();

        void Start() {
            var playerTransform = PlayerManager.LocalPlayer.transform;
            foreach(var itemSlot in UIManager.Instance.GetComponentsInChildren(typeof(UI_ItemSlot), true)) {
                var name = itemSlot.transform.parent.name;

                var slot = (UI_ItemSlot) itemSlot;

                itemSlots.Add(name, slot);
            }

            face = playerTransform.FindChild("face").GetComponent<ClothingItem>();
            body = playerTransform.FindChild("body").GetComponent<ClothingItem>();
            underwear = playerTransform.FindChild("underwear").GetComponent<ClothingItem>();

            SetPlayerLoadOuts();
        }

        void SetPlayerLoadOuts() {
            face.Reference = faceReference;
            body.Reference = bodyReference;
            underwear.Reference = underwearReference;

            SetItem("suit", suitPrefab);
            SetItem("belt", beltPrefab);
            SetItem("feet", shoesPrefab);
            SetItem("head", headPrefab);
            SetItem("mask", maskPrefab);
            SetItem("uniform", uniformPrefab);
            SetItem("neck", neckPrefab);
            SetItem("ear", earPrefab);
            SetItem("eyes", glassesPrefab);
            SetItem("hands", glovesPrefab);

            SetItem("id", idPrefab);
            SetItem("back", bagPrefab);
            SetItem("rightHand", rightHandPrefab);
            SetItem("leftHand", leftHandPrefab);
            SetItem("storage01", storage01Prefab);
            SetItem("storage02", storage02Prefab);
            SetItem("suitStorage", suitStoragePrefab);
        }

        private void SetItem(string eventName, GameObject prefab) {
            if(prefab != null) {

                GameObject item = PhotonNetwork.Instantiate(prefab.name, Vector3.zero, Quaternion.identity, 0, null);

                if(eventName.Length > 0)
                    EventManager.UI.TriggerEvent(eventName, item);
            }
        }
    }
}
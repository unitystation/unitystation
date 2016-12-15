using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI {

    class ClothingSlotTuple {
        public UI_ItemSlot itemSlot;
        public ClothingItem clothingItem;

        public ClothingSlotTuple(UI_ItemSlot itemSlot, ClothingItem clothingItem) {
            this.itemSlot = itemSlot;
            this.clothingItem = clothingItem;
        }
    }

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

        private Dictionary<string, ClothingSlotTuple> clothingSlotTuples = new Dictionary<string, ClothingSlotTuple>();

        void Start() {
            var playerTransform = PlayerManager.control.LocalPlayer.transform;
            foreach(var itemSlot in UIManager.control.GetComponentsInChildren(typeof(UI_ItemSlot), true)) {
                var name = itemSlot.transform.parent.name;

                var slot = (UI_ItemSlot) itemSlot;

                ClothingItem clothingItem = null;

                if(slot.hasClothing) {
                    clothingItem = playerTransform.FindChild(slot.clothingName).GetComponent<ClothingItem>();
                }

                clothingSlotTuples.Add(name, new ClothingSlotTuple(slot, clothingItem));
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

        public void UpdateEquipment(string slotName, GameObject item) {
            if(clothingSlotTuples.ContainsKey(slotName) && item != null) {

                clothingSlotTuples[slotName].itemSlot.SetItem(item);

                if(clothingSlotTuples[slotName].clothingItem != null) {
                    clothingSlotTuples[slotName].clothingItem.UpdateItem(item);
                }
            }
        }

        public void UpdateSlot(string slotName, GameObject item) {
            if(clothingSlotTuples.ContainsKey(slotName) && item != null) {

                clothingSlotTuples[slotName].itemSlot.SetItem(item);
            }
        }

        public void UpdateClothing(string slotName, GameObject item) {
            if(clothingSlotTuples.ContainsKey(slotName) && item != null) {
                if(clothingSlotTuples[slotName].clothingItem != null) {
                    clothingSlotTuples[slotName].clothingItem.UpdateItem(item);
                }
            }
        }

        public void ClearSlot(string slotName) {
            if(clothingSlotTuples.ContainsKey(slotName)) {

                clothingSlotTuples[slotName].itemSlot.RemoveItem();
            }
        }

        public void ClearClothing(string slotName) {
            Debug.Log(slotName);
            if(clothingSlotTuples.ContainsKey(slotName)) {
                if(clothingSlotTuples[slotName].clothingItem != null) {
                    clothingSlotTuples[slotName].clothingItem.Clear();
                }
            }
        }

        private void SetItemSlot(string slotName, GameObject prefab) {
            if(clothingSlotTuples.ContainsKey(slotName) && prefab != null) {
                if (Managers.control.isDevMode)
                {
                    var item = Instantiate(prefab);
                    clothingSlotTuples[slotName].itemSlot.SetItem(item);

                    if (clothingSlotTuples[slotName].clothingItem != null)
                    {
                        clothingSlotTuples[slotName].clothingItem.UpdateItem(item);
                    
                    }
                }
                else
                {
                    var item = PhotonNetwork.InstantiateSceneObject(prefab.name,Vector3.zero,Quaternion.identity,0,null);
                    clothingSlotTuples[slotName].itemSlot.SetItem(item);

                    if (clothingSlotTuples[slotName].clothingItem != null)
                    {
                        clothingSlotTuples[slotName].clothingItem.UpdateItem(item);

                    }
                
                }
            }
        }
    }
}
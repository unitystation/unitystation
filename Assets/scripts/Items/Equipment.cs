using Events;
using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace UI {

	public class Equipment: NetworkBehaviour {
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
            foreach(var itemSlot in UIManager.Instance.GetComponentsInChildren(typeof(UI_ItemSlot), true)) {
                var name = itemSlot.transform.parent.name;

                var slot = (UI_ItemSlot) itemSlot;

                itemSlots.Add(name, slot);
            }

            face = transform.FindChild("face").GetComponent<ClothingItem>();
            body = transform.FindChild("body").GetComponent<ClothingItem>();
            underwear = transform.FindChild("underwear").GetComponent<ClothingItem>();

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
			if(prefab != null && isLocalPlayer) {
				CmdSetItem(eventName, prefab, GetComponent<NetworkIdentity>());
            }
        }

		[Command]
		void CmdSetItem(string eventName, GameObject prefab, NetworkIdentity ID){
//			GameObject item = Instantiate(prefab, Vector3.zero, Quaternion.identity, null);
			if (isServer) {
				Debug.Log("FIXEME: Work out how to spawn clotheItems from server");
//				NetworkServer.Spawn(item);
			}
//			RpcSetUI(eventName, item, ID.netId);
		}

		[ClientRpc]
		void RpcSetUI(string eventName, GameObject item, NetworkInstanceId ID){
			if (netId == ID) {
				if(eventName.Length > 0)
					EventManager.UI.TriggerEvent(eventName, item);
			}
		}
    }
}
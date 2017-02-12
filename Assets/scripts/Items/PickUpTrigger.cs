using UnityEngine;
using System.Collections;
using PlayGroup;

namespace Items {
    public class PickUpTrigger: MonoBehaviour {
        public bool allowedPickUp = true;
		private GameObject uiTriggerObj;

//		void Awake(){
//			//To fix the issue where triggers become extremely small when adding to UI
//			uiTriggerObj = new GameObject();
//			uiTriggerObj.transform.parent = transform;
//			uiTriggerObj.name = "UI_Trigger";
//			BoxCollider2D coll = uiTriggerObj.AddComponent<BoxCollider2D>();
//			coll.isTrigger = true;
//			coll.size = new Vector2(10f, 10f);
//			uiTriggerObj.SetActive(false);
//
//		}

        void OnMouseDown() {
            Debug.Log("CLICKED " + gameObject.name);

            if(PlayerManager.PlayerInReach(transform) && allowedPickUp) {
                // try to add the item to hand
                ItemManager.control.TryToPickUpObject(gameObject);
            }
        }

		public void OnAddToInventory(){
			if(uiTriggerObj != null)
			uiTriggerObj.SetActive(true);
		}

		public void OnRemoveFromInventory(){
			uiTriggerObj.SetActive(false);
			gameObject.transform.localScale = Vector3.one;
		}

    }
}

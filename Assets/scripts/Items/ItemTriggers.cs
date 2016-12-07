using UnityEngine;
using System.Collections;
using PlayGroup;

namespace Items {
    public class ItemTriggers: MonoBehaviour {
        public bool allowedPickUp = true;

        void Start() {

        }
        
        void Update() {

        }

        void OnMouseDown() {
            Debug.Log("hello ");
            if(PlayerManager.control.playerScript != null) {
                var headingToPlayer = PlayerManager.control.playerScript.transform.position - transform.position;
                var distance = headingToPlayer.magnitude;
                var direction = headingToPlayer / distance;
                Debug.Log("hello !");

                if(distance <= 2f && allowedPickUp) {
                    // try to add the item to hand
                    var r = ItemManager.control.TryToPickUpObject(this.gameObject);

                    Debug.Log("hello " + r);

                }
            }
        }
    }
}

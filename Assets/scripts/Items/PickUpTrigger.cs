using UnityEngine;
using System.Collections;
using PlayGroup;

namespace Items {
    public class PickUpTrigger: MonoBehaviour {
        public bool allowedPickUp = true;

        void OnMouseDown() {
            if(PlayerManager.control.playerScript != null) {
                var distanceToPlayer = PlayerManager.control.playerScript.DistanceTo(transform.position);
                if(distanceToPlayer <= 2f && allowedPickUp) {
                    // try to add the item to hand
                    ItemManager.control.TryToPickUpObject(this.gameObject);
                }
            }
        }
    }
}

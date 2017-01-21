using UnityEngine;
using System.Collections;
using PlayGroup;

namespace Items {
    public class PickUpTrigger: MonoBehaviour {
        public bool allowedPickUp = true;

        void OnMouseDown() {
            Debug.Log("CLICKED " + gameObject.name);

            if(PlayerManager.PlayerInReach(transform) && allowedPickUp) {
                // try to add the item to hand
                ItemManager.control.TryToPickUpObject(gameObject);
            }
        }

    }
}

using UnityEngine;
using System.Collections;
using PlayGroup;
using UI;

namespace Items {

    public class ItemManager: MonoBehaviour {
        public static ItemManager control;

        void Awake() {
            if(control == null) {
                control = this;
            } else {
                Destroy(this);
            }
        }

        public bool TryToPickUpObject(GameObject itemObject) {            
            if(PlayerManager.PlayerScript != null) {
                
                if(!UIManager.Hands.CurrentSlot.TrySetItem(itemObject))
                    return false;
            } else {
                return false;
            }
            
            return true;
        }
    }
}

public enum SwitchDirection{
	up,
	down,
	left,
	right
}

using UnityEngine;
using System.Collections;
using PlayGroup;
using UI;

namespace Items {

    public class ItemManager: MonoBehaviour {

        private static ItemManager itemManager;
        public static ItemManager Instance {
            get {
                if(!itemManager) {
                    itemManager = FindObjectOfType<ItemManager>();
                }

                return itemManager;
            }
        }

        public static bool TryToPickUpObject(GameObject itemObject) {            
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
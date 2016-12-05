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
        
        void Start() {
        }
        
        void Update() {
        }

        public bool TryToPickUpObject(GameObject itemObject) {            
            if(PlayerManager.control.playerScript != null) {
                
                if(!UIManager.control.hands.currentSlot.TryToAddItem(itemObject))
                    return false;
            } else {
                return false;
            }

            ItemAttributes attributes = itemObject.GetComponent<ItemAttributes>();

            PlayerSprites pSprites = PlayerManager.control.playerScript.playerSprites;
            pSprites.PickedUpItem(itemObject);


            //TODO: communicate with playersprites and give it a reference to the items
            //TODO: carring sprites (lefthand, righthand etc). Remember to check if it is
            //TODO: right or left hand aswell.

            return true;
        }

        public GameObject RemoveItemFromHand() {
            var pSprites = PlayerManager.control.playerScript.playerSprites;
            return pSprites.RemoveCurrentItemFromHand();
        }
    }
}

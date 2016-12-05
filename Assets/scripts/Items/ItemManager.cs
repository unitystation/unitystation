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

            ItemAttributes attributes = itemObject.GetComponent<ItemAttributes>();
            PlayerSprites pSprites = PlayerManager.control.playerScript.playerSprites;

            //determine what hand is selected and if it is full
            if(PlayerManager.control.playerScript != null) {

                bool success = false;

                if(UIManager.control.isRightHand) {
                    //move the whole item into the hand
                    success = UIManager.control.hands.rightSlot.TryToAddItem(itemObject);
                } else {
                    success = UIManager.control.hands.leftSlot.TryToAddItem(itemObject);
                }

                if(!success)
                    return false;
            } else {
                return false;
            }

            pSprites.PickedUpItem(itemObject); //hard coded to kitchen knife data temporarily


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

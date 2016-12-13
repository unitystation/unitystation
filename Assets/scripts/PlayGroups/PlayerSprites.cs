using UnityEngine;
using System.Collections;
using UI;
using System.Collections.Generic;

namespace PlayGroup {
    public class PlayerSprites: MonoBehaviour {
        private Vector2 currentDirection;
        private Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

        void Awake() {
            foreach(var c in GetComponentsInChildren<ClothingItem>()) {
                clothes[c.name] = c;
            }
            FaceDirection(Vector2.down);
        }

        public void SetSprites(Dictionary<string, int> pref) {
            foreach(var c in clothes.Values) {
                if(pref.ContainsKey(c.spriteSheetName))
                    c.Reference = pref[c.spriteSheetName];
            }
        }

        //turning character input and sprite update
        public void FaceDirection(Vector2 direction) {
            if(currentDirection != direction) {
                foreach(var c in clothes.Values) {
                    c.Direction = direction;
                }

                currentDirection = direction;
            }
        }

        //REAL SHIT METHOD FIX IT LATER OKAY - doobly
        public void PickedUpItem(GameObject item) {
            ItemAttributes att = item.GetComponent<ItemAttributes>();

            //FIXME No longer works over photon network. Need to sync the picked up item over photon and then only handle change of 
            // direction based on that character and what hand that item is in on that specific character also

            if(UIManager.control.isRightHand) {
                clothes["rightHand"].UpdateReference(att);
            } else {
                clothes["leftHand"].UpdateReference(att);
            }
        }

        public void RemoveItemFromHand(bool rightHand) {
            if(rightHand) {
                clothes["rightHand"].Clear();
            } else {
                clothes["leftHand"].Clear();
            }
        }
    }
}
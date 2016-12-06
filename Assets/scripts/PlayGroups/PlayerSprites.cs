using UnityEngine;
using System.Collections;
using UI;
using System.Collections.Generic;

namespace PlayGroup {
    public class PlayerSprites: MonoBehaviour {
        private Vector2 currentDirection;
        private new Rigidbody2D rigidbody;
        private Dictionary<string, ClothingItem> clothes = new Dictionary<string, ClothingItem>();

        // Use this for initialization
        void Start() {
            rigidbody = GetComponent<Rigidbody2D>();
                        
            foreach(var c in GetComponentsInChildren<ClothingItem>()) {
                clothes[c.name] = c;
            }

            currentDirection = Vector2.down;
        }
        
        public void SetSprites(Dictionary<string, int> pref) {
            foreach(var c in clothes.Values) {
                if(pref.ContainsKey(c.spriteSheetName))
                    c.Reference = pref[c.spriteSheetName];
            }
        }

        void FixedUpdate() {
            if(rigidbody != null) {
                var localVel = transform.InverseTransformDirection(rigidbody.velocity);
                
                if(localVel.x > 1f) {

                    FaceDirection(Vector2.right);
                }
                if(localVel.x < -1f) {

                    FaceDirection(Vector2.left);
                }
                if(localVel.y < -1f) {

                    FaceDirection(Vector2.down);
                }
                if(localVel.y > 1f) {

                    FaceDirection(Vector2.up);
                }
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
        public void PickedUpItem(GameObject obj) {

            ItemAttributes att = obj.GetComponent<ItemAttributes>();

            //FIXME No longer works over photon network. Need to sync the picked up item over photon and then only handle change of 
            // direction based on that character and what hand that item is in on that specific character also
                        
            if(UIManager.control.isRightHand) {
                clothes["rightHand"].Reference = att.inHandReferenceRight;
            } else {
                clothes["leftHand"].Reference = att.inHandReferenceLeft;    
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
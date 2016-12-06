using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI {
    public class Hands: MonoBehaviour {
        //Handles left and right hand + selector

        // graphic for selection
        public GameObject selector;

        // hands
        public GameObject leftHand;
        public GameObject rightHand;

        //item slots
        [HideInInspector]
        public UI_ItemSlot currentSlot;
        public UI_ItemSlot leftSlot;
        public UI_ItemSlot rightSlot;

        //components
        [HideInInspector]
        public HandActions actions;

        // Use this for initialization
        void Start() {
            actions = gameObject.AddComponent<HandActions>();
            currentSlot = rightSlot;
        }

        // whether selector should be on the right hand or the left
        public void SelectorState(bool isRight) {
            if(UIManager.control != null) {
                PlayClick01();

                if(isRight) {
                    selector.transform.position = rightHand.transform.position;
                    currentSlot = rightSlot;

                } else {
                    selector.transform.position = leftHand.transform.position;
                    currentSlot = leftSlot;

                }
            }
        }

        //For swap button
        public void Swap() {
            if(currentSlot == rightSlot) {
                SelectorState(false);
            } else {
                SelectorState(true);
            }
        }
        
        /* 
		 * Button OnClick methods
		 */
         
        //OnClick Methods
        public void Use() {
            PlayClick01();

        }

        //		public void CheckAction(bool rightHand){
        //		
        //			handActions.Check (rightHand); //check if it should be used or given to other hand
        //		}

        //SoundFX

        void PlayClick01() {

            if(SoundManager.control != null) {
                SoundManager.control.sounds["Click01"].Play();
            }

        }
    }
}

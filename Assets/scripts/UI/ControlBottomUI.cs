using UnityEngine;
using System.Collections;

namespace UI{
public class ControlBottomUI : MonoBehaviour {

        [HideInInspector]
        public UI_ItemSlot beltSlot;
        [HideInInspector]
        public UI_ItemSlot bagSlot;
        [HideInInspector]
        public UI_ItemSlot IDSlot;
        [HideInInspector]
        public UI_ItemSlot suitStorageSlot;
        [HideInInspector]
        public UI_ItemSlot storage01Slot;
        [HideInInspector]
        public UI_ItemSlot storage02Slot;

        void Start() {
            beltSlot = transform.FindChild("Belt").FindChild("itemSlot").GetComponent<UI_ItemSlot>();
            bagSlot = transform.FindChild("Bag").FindChild("itemSlot").GetComponent<UI_ItemSlot>();
            IDSlot = transform.FindChild("ID").FindChild("itemSlot").GetComponent<UI_ItemSlot>();
            suitStorageSlot = transform.FindChild("SuitStorage").FindChild("itemSlot").GetComponent<UI_ItemSlot>();
            storage01Slot = transform.FindChild("Storage01").FindChild("itemSlot").GetComponent<UI_ItemSlot>();
            storage02Slot = transform.FindChild("Storage02").FindChild("itemSlot").GetComponent<UI_ItemSlot>();
        }

		/* 
		 * Button OnClick methods
		 */

		public void SuitStorage(){
			PlayClick01 ();
            suitStorageSlot.TryToSwapItem(UIManager.control.hands.currentSlot);

        }

		public void ID(){
			PlayClick01 ();
            IDSlot.TryToSwapItem(UIManager.control.hands.currentSlot);

        }

		public void Belt(){
			PlayClick01 ();
            Debug.Log("Belt");
            beltSlot.TryToSwapItem(UIManager.control.hands.currentSlot);

        }

		public void Bag(){
			PlayClick01 ();
			//FIXME remove after adding the bag control class
			// that will also control the inventory
			Debug.Log ("Bag");

		}

		public void Storage01(){
			PlayClick01 ();
            storage01Slot.TryToSwapItem(UIManager.control.hands.currentSlot);
		}

		public void Storage02(){
			PlayClick01 ();
            storage02Slot.TryToSwapItem(UIManager.control.hands.currentSlot);
		}
			

		//SoundFX

		void PlayClick01(){

			if (SoundManager.control != null) {
                SoundManager.control.Play("Click01");
            }

		}
}
}
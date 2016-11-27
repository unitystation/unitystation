using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Items;

namespace UI{

	public enum SlotType{

		rightHand,
		leftHand,
		storage01,
		storage02

	}
	
public class UI_ItemSlot : MonoBehaviour {

		public bool isFull{ get{ if (inHandItem == null) {
					                 return false;
		                             } else {
			                         return true;
		                             } } }
		public GameObject inHandItem;
		public SlotType thisSlot;


	// Use this for initialization
	void Start () {
			

	}

		public void AddItem(GameObject itemObj){
		
			if (!isFull) {
				
				ItemUI_Tracker itemTracker = itemObj.GetComponent<ItemUI_Tracker> ();
				if (itemTracker == null) {
					itemTracker = itemObj.AddComponent<ItemUI_Tracker> ();
				}
				itemTracker.slotType = thisSlot;

				inHandItem = itemObj;
				itemObj.transform.position = transform.position;
				itemObj.transform.parent = this.gameObject.transform;



			
			} else {
			
				Debug.Log ("This slot is full, you cannot put an item here");
			
			}
		
		}

	
	

}
}
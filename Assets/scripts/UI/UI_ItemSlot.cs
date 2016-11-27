using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Items;

namespace UI{
	
public class UI_ItemSlot : MonoBehaviour {

		public bool isFull{ get{ if (inHandItem == null) {
					                 return false;
		                             } else {
			                         return true;
		                             } } }
		public GameObject inHandItem;


	// Use this for initialization
	void Start () {
			

	}

		public void AddItem(GameObject itemObj){
		
			if (!isFull) {
			
				inHandItem = itemObj;
				itemObj.transform.position = transform.position;
				itemObj.transform.parent = this.gameObject.transform;

				ItemUI_Tracker itemTracker = itemObj.GetComponent<ItemUI_Tracker> ();
				if (itemTracker == null) {
					itemObj.AddComponent<ItemUI_Tracker> ();
				}

			
			} else {
			
				Debug.Log ("This slot is full, you cannot put an item here");
			
			}
		
		}

	
	

}
}
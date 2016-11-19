using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace UI{
	[RequireComponent (typeof (Image))]
public class UI_ItemSlot : MonoBehaviour {


		private Image thisImg;

	// Use this for initialization
	void Start () {
			thisImg = GetComponent<Image> ();
			thisImg.sprite = null;
			thisImg.enabled = false;
	}
	
		//as mentioned, using sprites for time being until items implemented
		public void AddItem(Sprite item){
			if (thisImg.sprite == null) {
				//Item Adding allowed
				thisImg.sprite = item;
				thisImg.enabled = true;
			
			} else {
			
				Debug.Log ("Cannot add item, slot is full");
			
			}
		
		}

		public Sprite GetItem(){


			return thisImg.sprite;
		}

		public bool isFull(){
		
			Sprite itemCheck = thisImg.sprite;
			if (itemCheck != null) {
				return true;
			} else {
				return false;
			}
		
		}

		public void Clear(){

			thisImg.sprite = null;
			thisImg.enabled = false;

		}

}
}
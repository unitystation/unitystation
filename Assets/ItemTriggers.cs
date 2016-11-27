using UnityEngine;
using System.Collections;

namespace Items{
public class ItemTriggers : MonoBehaviour {

		public bool allowedPickUp;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

		void OnTriggerEnter2D (Collider2D coll){

			if (coll.gameObject.layer == 8) {
				//Players layer
				allowedPickUp = true;

			}

		}

		void OnTriggerExit2D (Collider2D coll){

			if (coll.gameObject.layer == 8) {
				//Players layer
				allowedPickUp = false;


			}

		}

		void OnMouseDown(){
			Debug.Log ("MOUSE DOWN");
			//and eventually check if there is room in the hand etc
			if (allowedPickUp) {
				//add to hand
				ItemManager.control.PickUpObject(this.gameObject);


			}

		}
}
}

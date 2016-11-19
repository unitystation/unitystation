using UnityEngine;
using System.Collections;
using SS.PlayGroup;
using UI;

namespace SS.Kitchen{
public class KitchenItem : MonoBehaviour {

		// Temp code, under construction
		// The idea is to create item specific components that will move using transform move functions as physics is not
		// needed for movement or triggers

	public SpriteRenderer spriteRend;
	private Sprite[] spriteSheet;
	private Rigidbody2D thisRigi;

		//temp, pick a sprite number from kitchensprite sheet play around with it
	public int itemNumber;

	//all public for debug until basic functions worked out to be later moved dedicated item components
	public bool allowedPickUp;


      //TODO: create ItemPhysics component
 
	// Use this for initialization
	void Start () {


			thisRigi = GetComponent<Rigidbody2D> ();
			spriteSheet = Resources.LoadAll<Sprite> ("obj/kitchen");
			if (spriteSheet == null) {
				Debug.LogError ("DID NOT LOAD SPRITESHEET");
			}

			//set item
			spriteRend.sprite = spriteSheet[itemNumber];

	}
	
	// Update is called once per frame
	void Update () {
	
	}

	//temp triggers and events, will add this to a dedicated item component after dev
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

			//and eventually check if there is room in the hand etc
			if (allowedPickUp) {
			//add to hand
				UIManager.control.PickUpObject(spriteRend.sprite, itemNumber); //HARD WIRED ATM FIX THIS SHITE - doobly
		
				gameObject.SetActive (false);
			
			}

		}
}
}

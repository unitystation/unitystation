using UnityEngine;
using System.Collections;
using SS.PlayGroup;

public class DoorTile : MonoBehaviour {

	/* QUICK MOCK UP
	 * OF DOOR SYSTEM
	 */

	public Animator thisAnim;
	public BoxCollider2D boxColl;
	private bool isOpened = false;

	// Use this for initialization


	public void BoxCollToggleOn(){

			boxColl.enabled = true;

	}

	public void BoxCollToggleOff(){

		boxColl.enabled = false;

	}
	
	void OnTriggerEnter2D(Collider2D coll){

		if (PlayerScript.playerControl != null) {
			if (coll.gameObject == PlayerScript.playerControl.gameObject && !isOpened) {
				isOpened = true;
				thisAnim.SetBool ("open", true);
				SoundController.control.sounds [1].Play ();
			
			
			}
		
		
		}

	}


	void OnTriggerExit2D(Collider2D coll){

		if (PlayerScript.playerControl != null) {
			if (coll.gameObject == PlayerScript.playerControl.gameObject && isOpened) {
				isOpened = false;
				thisAnim.SetBool ("open", false);
				SoundController.control.sounds [2].Play ();


			}


		}
	
	}
}

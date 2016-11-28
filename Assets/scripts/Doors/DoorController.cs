using UnityEngine;
using System.Collections;
using SS.PlayGroup;

public class DoorController : MonoBehaviour {

	/* QUICK MOCK UP
	 * OF DOOR SYSTEM
	 */

	public Animator thisAnim;
	public BoxCollider2D boxColl;
	private bool isOpened = false;
	public string idleState;

	// Use this for initialization
	void Start(){

		thisAnim.Play (idleState);

	}

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
				SoundManager.control.sounds [1].Play ();
			
			
			}
		
		
		}

	}


	void OnTriggerExit2D(Collider2D coll){

		if (PlayerScript.playerControl != null) {
			if (coll.gameObject == PlayerScript.playerControl.gameObject && isOpened) {
				isOpened = false;
				thisAnim.SetBool ("open", false);
			}
		}
	
	}

	public void PlayCloseSound(){
		
			SoundManager.control.sounds [2].time = 0;
			SoundManager.control.sounds [2].Play ();

	}

	public void PlayCloseSFXshort(){

		if (SoundManager.control != null) {
			SoundManager.control.sounds [2].time = 0.6f;
			SoundManager.control.sounds [2].Play ();
		}
	}


}

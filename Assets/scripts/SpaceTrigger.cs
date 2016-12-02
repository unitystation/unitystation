using UnityEngine;
using System.Collections;
using PlayGroup;

public class SpaceTrigger : MonoBehaviour {

	private bool isSpaced = false;

	//TEMP MOCK UP

	void OnTriggerEnter2D (Collider2D coll){
	
	
		if (PlayerManager.control.playerScript != null) {
		
			if (coll.gameObject == PlayerManager.control.playerScript.gameObject && !isSpaced) {
			
				isSpaced = true;
				PlayerManager.control.playerScript.physicsMove.isSpaced = true;
			
			}
		
		
		}
	
	}
}

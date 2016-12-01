using UnityEngine;
using System.Collections;
using PlayGroup;

public class SpaceTrigger : MonoBehaviour {

	private bool isSpaced = false;

	//TEMP MOCK UP

	void OnTriggerEnter2D (Collider2D coll){
	
	
		if (Managers.control.playerScript != null) {
		
			if (coll.gameObject == Managers.control.playerScript.gameObject && !isSpaced) {
			
				isSpaced = true;
				Managers.control.playerScript.physicsMove.isSpaced = true;
			
			}
		
		
		}
	
	}
}

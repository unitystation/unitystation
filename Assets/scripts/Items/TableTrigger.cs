using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class TableTrigger : MonoBehaviour {
    
	void Start () {
		
	}

    void OnMouseDown() {
        if(PlayerManager.control.playerScript != null) {
            var headingToPlayer = PlayerManager.control.playerScript.transform.position - transform.position;
            var distance = headingToPlayer.magnitude;

            if(distance <= 2f) {
                GameObject item = UIManager.control.hands.currentSlot.RemoveItem();
                if(item != null) {
                    var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    targetPosition.z = -0.2f;
                    item.transform.position = targetPosition;
                    item.transform.parent = transform;
                }
            }
        }
    }
}

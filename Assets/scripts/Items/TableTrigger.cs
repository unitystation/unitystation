using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class TableTrigger: MonoBehaviour {

    void OnMouseDown() {
        if(PlayerManager.PlayerInReach(transform)) {
			
			GameObject item = UIManager.Hands.CurrentSlot.Clear();
            if(item != null) {
                var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                targetPosition.z = -0.2f;
				PlayerManager.LocalPlayerScript.playerUI.CmdPlaceItem(UIManager.Hands.CurrentSlot.eventName, targetPosition, gameObject);

                item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
//
            }
        }
    }
}

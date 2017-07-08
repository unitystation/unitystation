using PlayGroup;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using InputControl;

public class TableTrigger: InputTrigger {
	public override void Interact() {
        if (PlayerManager.LocalPlayerScript != null)
            if (!PlayerManager.LocalPlayerScript.playerMove.allowInput || PlayerManager.LocalPlayerScript.playerMove.isGhost)
                return;

        if (PlayerManager.PlayerInReach(transform)) {
			GameObject item = UIManager.Hands.CurrentSlot.PlaceItemInScene();
			if(item != null) {
				var targetPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
				targetPosition.z = -0.2f;
				PlayerManager.LocalPlayerScript.playerNetworkActions.CmdPlaceItem(UIManager.Hands.CurrentSlot.eventName, targetPosition, gameObject);
				item.BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);
			}
		}
	}
}

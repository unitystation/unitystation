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
                item.transform.position = targetPosition;
                item.transform.parent = transform;

                BroadcastMessage("OnRemoveFromInventory", null, SendMessageOptions.DontRequireReceiver);

            }
        }
    }
}

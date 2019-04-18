using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class SpaceCleanerTrigger : PickUpTrigger
{
    public GameObject spray;

    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact(originator, position, hand);
        }

        var targetWorldPos = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
        if (PlayerManager.PlayerScript.IsInReach(targetWorldPos))
        {
            if (!isServer)
            {
                InteractMessage.Send(gameObject, hand);
            }
            else
            {
                //Play sound and call spray
                SoundManager.PlayNetworkedAtPos("Spray", targetWorldPos);
                ReagentContainer cleanerContainer = GetComponent<ReagentContainer>();
                StartCoroutine(Spray.TriggerSpray(cleanerContainer, targetWorldPos, spray));
            }
        }

        return base.Interact(originator, position, hand);
    }
}

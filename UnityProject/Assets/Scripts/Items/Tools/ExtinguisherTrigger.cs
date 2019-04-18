using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class ExtinguisherTrigger : PickUpTrigger
{
    public GameObject extinguish;
    private Extinguisher extinguisher;

    private void Start()
    {
        extinguisher = GetComponent<Extinguisher>();
    }

    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact(originator, position, hand);
        }

        var targetWorldPos = Camera.main.ScreenToWorldPoint(CommonInput.mousePosition);
        if (PlayerManager.PlayerScript.IsInReach(targetWorldPos) && extinguisher.isOn)
        {
            if (!isServer)
            {
                InteractMessage.Send(gameObject, hand);
            }
            else
            {
                //Play sound and call spray
                SoundManager.PlayNetworkedAtPos("Extinguish", targetWorldPos);
                ReagentContainer cleanerContainer = GetComponent<ReagentContainer>();
                StartCoroutine(Spray.TriggerSpray(cleanerContainer, targetWorldPos, extinguish));
            }
        }

        return base.Interact(originator, position, hand);
    }

    //For openning/closing fire extinguisher
    public override void UI_Interact(GameObject originator, string hand)
    {
        base.UI_Interact(originator, hand);

        if (!isServer)
        {
            UIInteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
        }
        else
        {
            //Toggle the extinguisher:
            extinguisher.ToggleExtinguisher(originator);
        }
    }
}

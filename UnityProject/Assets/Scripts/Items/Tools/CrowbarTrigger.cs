using UnityEngine;
using UnityEngine.Networking;

public class CrowbarTrigger : PickUpTrigger
{
    public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact (originator, position, hand);
        }
        var targetWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        return base.Interact (originator, position, hand);
    }
}
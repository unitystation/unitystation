using Items;
using UI;
using UnityEngine;

public class CrowbarTrigger : PickUpTrigger
{
    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            base.Interact(originator, position, hand);
            return;
        }

        base.Interact(originator, position, hand);
    }
}
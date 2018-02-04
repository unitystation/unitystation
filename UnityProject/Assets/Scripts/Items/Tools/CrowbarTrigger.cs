using Items;
using UI;
using UnityEngine;

public class CrowbarTrigger : PickUpTrigger
{
    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        //Only peform crobar actions when holding the crobar
        //TODO:  This should not be necessary?  
        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            base.Interact(originator, position, hand);
            return;
        }

        base.Interact(originator, position, hand);
    }
}
using UnityEngine;

public class WrenchTrigger : PickUpTrigger
{
    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact(originator, position, hand);
        }

        return base.Interact(originator, position, hand);
    }
}
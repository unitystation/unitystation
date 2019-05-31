using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class WrenchTrigger : InputTrigger
{
    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        //TODO: Remove after IF2 refactor

        return false;
    }
}
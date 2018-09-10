using UnityEngine;
using UnityEngine.Networking;

public class CrowbarTrigger : PickUpTrigger
{
    [SyncVar]
    private bool canBeUsed = false;

    public override void Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            base.Interact (originator, position, hand);
            return;
        }

        if (canBeUsed)
        {
            //TODO: Check inreach
        }

        base.Interact (originator, position, hand);
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnAddToPool ()
    {
        canBeUsed = true;
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnRemoveFromPool ()
    {
        canBeUsed = false;
    }
}
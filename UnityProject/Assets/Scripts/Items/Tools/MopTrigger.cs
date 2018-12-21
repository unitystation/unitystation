using UnityEngine;
using UnityEngine.Networking;

public class MopTrigger : PickUpTrigger
{
	[SyncVar]
	private bool canBeUsed = false;

    public override bool Interact (GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            return base.Interact (originator, position, hand);
        }
        var targetWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (canBeUsed && PlayerManager.PlayerScript.IsInReach(targetWorldPos))
        {
            //TODO INSERT CLEANING FUNCTION
        }

        return base.Interact (originator, position, hand);
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnAddToPool ()
    {
        canBeUsed = true;
    }

    //Broadcast from EquipmentPool.cs **ServerSide**
    public void OnRemoveFromInventory ()
    {
        canBeUsed = false;
    }
}
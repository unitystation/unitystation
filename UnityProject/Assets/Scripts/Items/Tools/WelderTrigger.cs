using UnityEngine;

public class WelderTrigger : PickUpTrigger
{

    private Welder welder;

    private void Start()
    {
        welder = GetComponent<Welder>();
    }
    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        //make sure that interactmessage doesn't tell the server to pick it up for server player:
        if (originator == PlayerManager.LocalPlayer)
        {
            if (UIManager.Hands.CurrentSlot.Item != gameObject)
            {
                welder.heldByPlayer = originator;
                return base.Interact(originator, position, hand);
            }
        }
        return base.Interact(originator, position, hand);
    }

    public override void UI_Interact(GameObject originator, string hand)
    {
        base.UI_Interact(originator, hand);

        if (!isServer)
        {
            UIInteractMessage.Send(gameObject, UIManager.Hands.CurrentSlot.eventName);
        }
        else
        {
            //Toggle the welder:
            welder.ToggleWelder(originator);
        }
    }
}
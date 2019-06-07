using UnityEngine;

[RequireComponent(typeof(Pickupable))]
public class WelderTrigger : InputTrigger
{

    private Welder welder;

    private void Start()
    {
        welder = GetComponent<Welder>();
    }
    public override bool Interact(GameObject originator, Vector3 position, string hand)
    {
        //TODO: Remove after IF2 refactor

        return false;
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
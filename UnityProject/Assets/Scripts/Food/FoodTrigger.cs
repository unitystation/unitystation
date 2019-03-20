using UnityEngine;

public class FoodTrigger : PickUpTrigger
{

    private FoodBehaviour food;
    void Start()
    {
        food = GetComponent<FoodBehaviour>();
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
            food.TryEat();
        }
    }
}

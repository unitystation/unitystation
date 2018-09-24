using UnityEngine;

public class WelderTrigger : PickUpTrigger
{

    private Welder welder;

    private void Start()
    {
        welder = GetComponent<Welder>();
    }
    public override void Interact(GameObject originator, Vector3 position, string hand)
    {
        //TODO:  Fill this in.

        if (UIManager.Hands.CurrentSlot.Item != gameObject)
        {
            base.Interact(originator, position, hand);
            return;
        }

        Debug.Log("CLICKED TOOL");

        // base.Interact(originator, position, hand);
    }
}
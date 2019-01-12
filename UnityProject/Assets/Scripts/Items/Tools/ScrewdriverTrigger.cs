using UnityEngine;

public class ScrewdriverTrigger : PickUpTrigger
{
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//Only peform screwdriver actions on other things when holding the screwdriver
		if (UIManager.Hands.CurrentSlot.Item != gameObject)
		{
			return base.Interact(originator, position, hand);
		}

		//TODO detect the actual target of the interact, instead of requiring the headset to be in the other hand
		GameObject otherHandsItem = UIManager.Hands.OtherSlot.Item;

		if (otherHandsItem && otherHandsItem.GetComponent<Headset>())
		{
			//Remove encryption key
			UpdateHeadsetKeyMessage.Send(otherHandsItem);
		}

		return base.Interact(originator, position, hand);
	}
}
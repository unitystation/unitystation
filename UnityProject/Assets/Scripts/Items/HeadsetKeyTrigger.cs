using UnityEngine;
[RequireComponent(typeof(Pickupable))]
public class HeadsetKeyTrigger : InputTrigger
{
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		GameObject otherHandsItem = UIManager.Hands.OtherSlot.Item;

		if (otherHandsItem && otherHandsItem.GetComponent<Headset>())
		{
			UpdateHeadsetKeyMessage.Send(otherHandsItem, gameObject);
			return true;
		}

		return false;
	}
}
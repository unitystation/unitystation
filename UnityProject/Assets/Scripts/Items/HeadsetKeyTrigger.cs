using UnityEngine;

public class HeadsetKeyTrigger : PickUpTrigger
{
	public override bool Interact(GameObject originator, Vector3 position, string hand)
	{
		//Only peform EncryptionKey actions on other things when holding the encryptionkey
		if (UIManager.Hands.CurrentSlot.Item != gameObject)
		{
			return base.Interact(originator, position, hand);
		}

		GameObject otherHandsItem = UIManager.Hands.OtherSlot.Item;

		if (otherHandsItem && otherHandsItem.GetComponent<Headset>())
		{
			UpdateHeadsetKeyMessage.Send(otherHandsItem, gameObject);
		}

		return base.Interact(originator, position, hand);
	}
}
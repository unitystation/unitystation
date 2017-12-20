using Items;
using UI;
using UnityEngine;

public class EncryptionkeyTrigger : PickUpTrigger
{
	public override void Interact(GameObject originator, Vector3 position, string hand)
	{
		//Only peform Encryptionkey actions on other things when holding the encryptionkey
		if (UIManager.Hands.CurrentSlot.Item != gameObject)
		{
			base.Interact(originator, position, hand);
			return;
		}

		GameObject otherHandsItem = UIManager.Hands.OtherSlot.Item;

		if (otherHandsItem && otherHandsItem.GetComponent<Headset>())
		{
			AddEncryptionkeyMessage.Send(otherHandsItem, gameObject);
		}

		base.Interact(originator, position, hand);
	}
}
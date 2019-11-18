using UnityEngine;


// This manages access via ID cards.
public class AccessRestrictions : MonoBehaviour
{
	public Access restriction;

	public bool CheckAccess(GameObject Player)
	{
		IDCard card;
		ItemStorage playerStorage = Player.GetComponent<ItemStorage>();

		//this isn't a player. It could be an npc:
		if (playerStorage == null)
		{
			if ((int) restriction == 0)
			{
				return true;
			}
			return false;
		}

		// Check for an ID card
		var idId = playerStorage.GetNamedItemSlot(NamedSlot.id).ItemObject;
		var handId = playerStorage.GetActiveHandSlot().ItemObject;
		if (idId != null && idId.GetComponent<IDCard>() != null)
		{
			card = idId.GetComponent<IDCard>();
		}
		else if (handId != null &&
		         handId.GetComponent<IDCard>() != null)
		{
			card = handId.GetComponent<IDCard>();
		}
		else
		{
			// If there isn't one, see if we even need one
			if ((int) restriction == 0)
			{
				return true;
			}
			// If there isn't one and we don't need one, we don't open the door
			return false;
		}

		// If we have an ID, make sure we have access
		if ((int) restriction == 0)
		{
			return true;
		}
		if (card.HasAccess(restriction))
		{
			return true;
		}
		return false;
	}
}
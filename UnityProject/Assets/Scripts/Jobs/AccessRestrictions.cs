using AccessType;
using UnityEngine;


// This manages access via ID cards. 
public class AccessRestrictions : MonoBehaviour
{
	public Access restriction;

	public bool CheckAccess(GameObject Player, string hand)
	{
		IDCard card;
		PlayerNetworkActions PNA = Player.GetComponent<PlayerNetworkActions>();

		// Check for an ID card
		if (PNA.Inventory.ContainsKey("id") &&
		    PNA.Inventory["id"]?.GetComponent<IDCard>() != null)
		{
			card = PNA.Inventory["id"].GetComponent<IDCard>();
		}
		else if (PNA.Inventory.ContainsKey(hand) &&
		         PNA.Inventory[hand]?.GetComponent<IDCard>() != null)
		{
			card = PNA.Inventory[hand].GetComponent<IDCard>();
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
		if (card.accessSyncList.Contains((int) restriction))
		{
			return true;
		}
		return false;
	}
}
using Items.PDA;
using UnityEngine;


// This manages access via ID cards.
public class AccessRestrictions : MonoBehaviour
{
	public Access restriction;

	public bool CheckAccess(GameObject Player)
	{
		// If there isn't any restriction, grant access right away
		if ((int) restriction == 0) return true;

		//There is no player object being checked, default to false.
		if (Player == null) return false;


		var playerStorage = Player.GetComponent<ItemStorage>();
		//this isn't a player. It could be an npc. No NPC access logic at the moment
		if (playerStorage == null) return false;


		//check if active hand or equipped id cards have access
		if (CheckAccessCard(playerStorage.GetNamedItemSlot(NamedSlot.id).ItemObject)) return true;

		return CheckAccessCard(playerStorage.GetActiveHandSlot().ItemObject);
	}

	public bool CheckAccessCard(GameObject idCardObj)
	{
		if (idCardObj == null) return false;
		var idcard = idCardObj.GetComponent<IDCard>();
		var pda = idCardObj.GetComponent<PDALogic>();
		if (idcard != null) return idcard.HasAccess(restriction);
		// Not an ID card? Perhaps its a PDA.
		if (pda != null && pda.IdCard != null)
		{
			// Its a PDA, check the contents.
			return pda.IdCard.HasAccess(restriction);
		}
		//The hell did it detect then?
		return false;
	}
}
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
		if (idCardObj == null)
			return false;
		var idCard = GetIDCard(idCardObj);
		if (idCard)
		{
			return idCard.HasAccess(restriction);
		}
		return false;
	}

	public static IDCard GetIDCard(GameObject idCardObj)
	{
		var idCard = idCardObj.GetComponent<IDCard>();
		var pda = idCardObj.GetComponent<PDALogic>();
		if (idCard != null)
		{
			return idCard;
		}

		if (pda != null)
		{
			return pda.IDCard;
		}

		return null;
	}
}
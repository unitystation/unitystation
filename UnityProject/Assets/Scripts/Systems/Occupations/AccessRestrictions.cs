using Items.PDA;
using UnityEngine;


// This manages access via ID cards.
public class AccessRestrictions : MonoBehaviour
{
	public Access restriction;

	public bool CheckAccess(GameObject Player)
	{
		return CheckAccess(Player, restriction);
	}

	public bool CheckAccessCard(GameObject idCardObj)
	{
		return CheckAccessCard(idCardObj, restriction);
	}

	public static bool CheckAccess(GameObject Player, Access restriction)
	{
		// If there isn't any restriction, grant access right away
		if ((int) restriction == 0) return true;

		//There is no player object being checked, default to false.
		if (Player == null) return false;


		var playerStorage = Player.GetComponent<DynamicItemStorage>();
		//this isn't a player. It could be an npc. No NPC access logic at the moment
		if (playerStorage == null) return false;


		//check if active hand or equipped id cards have access
		foreach (var itemSlot in playerStorage.GetNamedItemSlots(NamedSlot.id))
		{
			if (CheckAccessCard(itemSlot.ItemObject, restriction)) return true;
		}

		return CheckAccessCard(playerStorage.GetActiveHandSlot()?.ItemObject, restriction);
	}

	public static bool CheckAccessCard(GameObject idCardObj, Access restriction)
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
		if (idCardObj.TryGetComponent<IDCard>(out var idCard))
		{
			return idCard;
		}

		if (idCardObj.TryGetComponent<PDALogic>(out var pda))
		{
			return pda.IDCard;
		}

		return null;
	}
}
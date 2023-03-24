using System;
using Items.PDA;
using Systems.Clearance;
using Systems.Clearance.Utils;
using UnityEngine;


// This manages access via ID cards.
public class AccessRestrictions : MonoBehaviour
{
	public Access restriction;

	//TODO Move doors over to use ClearanceRestrictions
	[NonSerialized]
	public Clearance clearanceRestriction = 0;

	public bool CheckAccess(GameObject player)
	{
		if (clearanceRestriction != 0)
		{
			return CheckAccess(player, clearanceRestriction);
		}

		return CheckAccess(player, MigrationData.Translation[restriction]);
	}

	public static bool CheckAccess(GameObject player, Clearance restriction)
	{
		// If there isn't any restriction, grant access right away
		if ((int) restriction == 0) return true;

		//There is no player object being checked, default to false.
		if (player == null) return false;


		var playerStorage = player.GetComponent<DynamicItemStorage>();
		//this isn't a player. It could be an npc. No NPC access logic at the moment
		if (playerStorage == null) return false;


		if (CheckAccessCard(playerStorage.GetActiveHandSlot()?.ItemObject, restriction)) return true;

		//check if active hand or equipped id cards have access
		foreach (var itemSlot in playerStorage.GetNamedItemSlots(NamedSlot.id))
		{
			if (CheckAccessCard(itemSlot.ItemObject, restriction)) return true;
		}

		return false;
	}

	public static bool CheckAccessCard(GameObject idCardObj, Clearance restriction)
	{
		if (idCardObj == null)
			return false;
		var idCard = GetIDCard(idCardObj);
		if (idCard)
		{
			// return idCard.HasAccess(restriction);
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
			return pda.GetIDCard();
		}

		return null;
	}
}
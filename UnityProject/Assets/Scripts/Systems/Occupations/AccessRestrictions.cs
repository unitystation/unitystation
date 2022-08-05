using Items.PDA;
using Systems.Clearance;
using Systems.Clearance.Utils;
using UnityEngine;


// This manages access via ID cards.
public class AccessRestrictions : MonoBehaviour
{
	public Access restriction;

	public Clearance clearanceRestriction;

	//TODO changing all doors to use new clearance will be a pain as it needs to be done per scene, need to make tool

	public bool CheckAccess(GameObject player)
	{
		return CheckAccess(player, MigrationData.Translation[restriction]);
	}

	public bool CheckAccessCard(GameObject idCardObj)
	{
		return CheckAccessCard(idCardObj, MigrationData.Translation[restriction]);
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


		//check if active hand or equipped id cards have access
		foreach (var itemSlot in playerStorage.GetNamedItemSlots(NamedSlot.id))
		{
			if (CheckAccessCard(itemSlot.ItemObject, restriction)) return true;
		}

		return CheckAccessCard(playerStorage.GetActiveHandSlot()?.ItemObject, restriction);
	}

	public static bool CheckAccessCard(GameObject idCardObj, Clearance restriction)
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
			return pda.GetIDCard();
		}

		return null;
	}
}
using System.Collections;
using System.Collections.Generic;
using Items;
using UnityEngine;

public class DamageOnPickUp : MonoBehaviour, IServerInventoryMove
{
	/// <summary>
	/// Does damage to active left or right arm.
	/// </summary>
	public bool doesDamage;

	/// <summary>
	/// 1 = 100%
	/// </summary>
	public float doesDamageChance = 1f;

	public float amountOfDamage = 10f;

	public AttackType attackType;

	public DamageType damageType;

	public ItemTrait[] protectionItemTraits;

	private PlayerScript player;

	public void OnInventoryMoveServer(InventoryMove info)
	{
		if (info.InventoryMoveType != InventoryMoveType.Add) return;

		if (info.ToSlot != null && info.ToSlot?.NamedSlot != null)
		{
			player = info.ToRootPlayer?.PlayerScript;

			if (player != null)
			{
				DoDamage(info);
			}
		}
	}

	private void DoDamage(InventoryMove info)
	{
		if (doesDamage && Random.value < doesDamageChance)
		{
			foreach (var trait in protectionItemTraits)
			{
				if (trait == null || Validations.HasItemTrait(player.Equipment.GetClothingItem(NamedSlot.hands).GameObjectReference, trait)) return;
			}

			if (info.ToSlot.NamedSlot == NamedSlot.leftHand)
			{
				player.playerHealth.ApplyDamageToBodypart(gameObject, amountOfDamage, attackType, damageType, BodyPartType.LeftArm);
			}
			else
			{
				player.playerHealth.ApplyDamageToBodypart(gameObject, amountOfDamage, attackType, damageType, BodyPartType.RightArm);
			}

			Chat.AddExamineMsgFromServer(player.gameObject, "<color=red>You injure yourself picking up the " + GetComponent<ItemAttributesV2>().ArticleName + "</color>");
		}
	}
}

using System.Collections;
using System.Collections.Generic;
using HealthV2;
using Items;
using Player;
using UnityEngine;

public class EyeFlash : BodyPartFunctionality
{
	public ItemTrait FlashProtection;

	[HideInInspector]
	public int WeldingShieldImplants = 0;

	public float FlashMultiplier = 1;

	public bool TryFlash(float flashDuration, bool checkForProtectiveCloth = true)
	{
		if (WeldingShieldImplants > 0)
		{
			return false;
		}

		if (RelatedPart.ItemAttributes.HasTrait(FlashProtection))
		{
			return false;
		}

		if (checkForProtectiveCloth)
		{
			if (HasProtectiveCloth())
			{
				return false;
			}
		}

		RelatedPart.TakeDamage(null, flashDuration*0.5f, AttackType.Energy, DamageType.Burn);
		PlayerFlashEffectsMessage.Send(RelatedPart.HealthMaster.gameObject, flashDuration  * FlashMultiplier);
		return true;
	}

	public bool HasProtectiveCloth()
	{
		if (RelatedPart.HealthMaster.TryGetComponent<DynamicItemStorage>(out var playerStorage) == false) return false;

		foreach (var slots in playerStorage.ServerContents)
		{
			//TODO Might be better for a script where you ask it if it's blocking Flash but this is good enough for now
			if (slots.Key != NamedSlot.eyes && slots.Key != NamedSlot.mask && slots.Key != NamedSlot.head) continue;
			foreach (ItemSlot onSlots in slots.Value)
			{
				if (onSlots.IsEmpty) continue;
				if (onSlots.ItemAttributes.HasTrait(FlashProtection))
				{
					return true;
				}
			}
		}
		return false;
	}
}

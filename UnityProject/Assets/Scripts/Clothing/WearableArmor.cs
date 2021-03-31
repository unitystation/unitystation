using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;

/// <summary>
/// allows clothing to add its armor values to the creature wearing it
/// </summary>
[RequireComponent(typeof(Integrity))]
public class WearableArmor : MonoBehaviour, IServerInventoryMove
{
	[SerializeField] [Tooltip("When wore in this slot, the armor values will be applied to player.")]
	private NamedSlot slot = NamedSlot.outerwear;

	[SerializeField] [Tooltip("What body parts does this item protect and how well does it protect.")]
	private List<ArmoredBodyPart> armoredBodyParts = new List<ArmoredBodyPart>();

	private PlayerHealthV2 playerHealthV2;


	[Serializable]
	public class ArmoredBodyPart
	{
		[SerializeField] private BodyPartType armoringBodyPartType;

		internal BodyPartType ArmoringBodyPartType => armoringBodyPartType;

		[SerializeField] private Armor armor;

		internal Armor Armor => armor;

		internal BodyPart bodyPartScript;
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//Wearing
		if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
		{
			playerHealthV2 = info.ToRootPlayer?.PlayerScript.playerHealth;

			if (playerHealthV2 != null && info.ToSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor(false);
			}
		}

		//taking off
		if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
		{
			playerHealthV2 = info.FromRootPlayer?.PlayerScript.playerHealth;

			if (playerHealthV2 != null && info.FromSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor(true);
			}
		}
	}

	/// <summary>
	/// Adds or removes armor per body part depending on the characteristics of this armor.
	/// </summary>
	/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
	private void UpdateBodyPartsArmor(bool currentlyRemovingArmor)
	{
		foreach (ArmoredBodyPart protectedBodyPart in armoredBodyParts)
		{
			foreach (RootBodyPartContainer rootBodyPartContainer in playerHealthV2.RootBodyPartContainers)
			{
				foreach (BodyPart bodyPart in rootBodyPartContainer.ContainsLimbs)
				{
					DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor);
				}
			}
		}
	}

	/// <summary>
	/// Adds or removes armor per body part depending on the characteristics of this armor.
	/// Checks not only the bodyPart, but also all other body parts nested in bodyPart.
	/// </summary>
	/// <param name="bodyPart">body part to update</param>
	/// <param name="armoredBodyPart">a tuple of the body part associated with the body part</param>
	/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
	/// <returns>true if the bodyPart was updated, false otherwise</returns>
	private static bool DeepUpdateBodyPartArmor(
		BodyPart bodyPart,
		ArmoredBodyPart armoredBodyPart,
		bool currentlyRemovingArmor
	)
	{
		if (bodyPart.BodyPartType != BodyPartType.None && bodyPart.BodyPartType == armoredBodyPart.ArmoringBodyPartType)
		{
			if (currentlyRemovingArmor)
			{
				bodyPart.ClothingArmors.Remove(armoredBodyPart.Armor);
				armoredBodyPart.bodyPartScript = null;
			}
			else
			{
				bodyPart.ClothingArmors.AddFirst(armoredBodyPart.Armor);
				armoredBodyPart.bodyPartScript = bodyPart;
			}

			return true;
		}

		foreach (BodyPart innerBodyPart in bodyPart.ContainBodyParts)
		{
			if (DeepUpdateBodyPartArmor(innerBodyPart, armoredBodyPart, currentlyRemovingArmor))
			{
				return true;
			}
		}

		return false;
	}
}

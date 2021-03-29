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
	public List<ProtectedBodyPart> armoredBodyPart = new List<ProtectedBodyPart>();

	private PlayerHealthV2 playerHealthV2;


	[Serializable]
	public class ProtectedBodyPart
	{
		public BodyPart bodyPart;
		public Armor armor;
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//Wearing
		if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
		{
			playerHealthV2 = info.ToRootPlayer?.PlayerScript.playerHealth;

			if (playerHealthV2 != null && info.ToSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor();
			}
		}

		//taking off
		if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
		{
			playerHealthV2 = info.FromRootPlayer?.PlayerScript.playerHealth;

			if (playerHealthV2 != null && info.FromSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor(currentlyRemovingArmor: true);
			}
		}
	}

	/// <summary>
	/// Adds or removes armor per body part depending on the characteristics of this armor.
	/// </summary>
	/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
	private void UpdateBodyPartsArmor(bool currentlyRemovingArmor = false)
	{
		foreach (Tuple<BodyPart, Armor> protectedBodyPart in armoredBodyPart)
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
	/// <param name="protectedBodyPart">a tuple of the body part associated with the body part</param>
	/// <param name="currentlyRemovingArmor">Are we taking off our armor or putting it on?</param>
	/// <returns>true if the bodyPart was updated, false otherwise</returns>
	private static bool DeepUpdateBodyPartArmor(
		BodyPart bodyPart,
		Tuple<BodyPart, Armor> protectedBodyPart,
		bool currentlyRemovingArmor
	)
	{
		if (protectedBodyPart.Item1 == bodyPart)
		{
			if (currentlyRemovingArmor)
			{
				bodyPart.ClothingArmor.Remove(protectedBodyPart.Item2);
			}
			else
			{
				bodyPart.ClothingArmor.AddFirst(protectedBodyPart.Item2);
			}
			return true;
		}

		if (protectedBodyPart.Item1.ContainBodyParts.Count == 0)
		{
			return false;
		}

		foreach (BodyPart innerBodyPart in protectedBodyPart.Item1.ContainBodyParts)
		{
			if (DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor))
			{
				return true;
			}
		}

		return false;
	}
}

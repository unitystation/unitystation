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
	private List<ProtectedBodyPart> armoredBodyParts = new List<ProtectedBodyPart>();

	private PlayerHealthV2 playerHealthV2;


	[Serializable]
	public class ProtectedBodyPart
	{
		[SerializeField]
		public GameObject bodyPartPrefab;
		[SerializeField]
		public Armor armor;

		internal BodyPart BodyPart = null;
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
		foreach (ProtectedBodyPart protectedBodyPart in armoredBodyParts)
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
		ProtectedBodyPart protectedBodyPart,
		bool currentlyRemovingArmor
	)
	{
		if (bodyPart.gameObject == protectedBodyPart.bodyPartPrefab.gameObject)
		{
			if (currentlyRemovingArmor)
			{
				bodyPart.ClothingArmor.Remove(protectedBodyPart.armor);
				protectedBodyPart.BodyPart = null;
			}
			else
			{
				bodyPart.ClothingArmor.AddFirst(protectedBodyPart.armor);
				protectedBodyPart.BodyPart = bodyPart;
			}
			return true;
		}

		if (protectedBodyPart.BodyPart.ContainBodyParts.Count == 0)
		{
			return false;
		}

		foreach (BodyPart innerBodyPart in protectedBodyPart.BodyPart.ContainBodyParts)
		{
			if (DeepUpdateBodyPartArmor(bodyPart, protectedBodyPart, currentlyRemovingArmor))
			{
				return true;
			}
		}

		return false;
	}

	/*
	 * using System;
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
	[SerializeField]
	[Tooltip("When wore in this slot, the armor values will be applied to player.")]
	private NamedSlot slot = NamedSlot.outerwear;

	[SerializeField]
	[Tooltip("What body parts does this item protect")]
	private BodyPartsCovered bodyPartsCovered = BodyPartsCovered.None;

	private PlayerHealthV2 player;
	private Armor armor;

	public void Awake()
	{
		armor = GetComponent<Integrity>().Armor;
	}

	public void OnInventoryMoveServer(InventoryMove info)
	{
		//Wearing
		if (info.ToSlot != null & info.ToSlot?.NamedSlot != null)
		{
			player = info.ToRootPlayer?.PlayerScript.playerHealth;

			if (player != null && info.ToSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor();
			}
		}
		//taking off
		if (info.FromSlot != null & info.FromSlot?.NamedSlot != null)
		{
			player = info.FromRootPlayer?.PlayerScript.playerHealth;

			if (player != null && info.FromSlot.NamedSlot == slot)
			{
				UpdateBodyPartsArmor(remove: true);
			}
		}
	}

	private void UpdateBodyPartsArmor(bool remove = false)
	{
		foreach(BodyPartsCovered coveredPart in bodyPartsCovered.GetFlags())
		{
			if (coveredPart == BodyPartsCovered.None)
			{
				continue;
			}

			//TODO: Reimplement adding armor to body parts.
			var bodyPart = bodyParts[coveredPart];
			foreach (var part in player.BodyParts.Where(part => part.Type == bodyPart))
			{
				if (remove)
				{
					part.armor -= armor;
					break;
				}
				else
				{
					part.armor += armor;
					break;
				}
			}
		}
	}

[Flags]
	private enum BodyPartsCovered
	{
		None = 0,
		Head = 1 << 1,
		Eyes = 1 << 2,
		Mouth = 1 << 3,
		Chest = 1 << 4,
		LeftArm = 1 << 5,
		RightArm = 1 << 6,
		Groin = 1 << 7,
		LeftLeg = 1 << 8,
		RightLeg = 1 << 9,
		RightHand = 1 << 10,
		LeftHand = 1 << 11,
		LeftFoot = 1 << 12,
		RightFoot = 1 << 13
	}

	private readonly Dictionary<BodyPartsCovered, BodyPartType> bodyParts = new Dictionary<BodyPartsCovered, BodyPartType> ()
	{
		{BodyPartsCovered.None, BodyPartType.None},
		{BodyPartsCovered.Head, BodyPartType.Head},
		{BodyPartsCovered.Eyes, BodyPartType.Eyes},
		{BodyPartsCovered.Mouth, BodyPartType.Mouth},
		{BodyPartsCovered.Chest, BodyPartType.Chest},
		{BodyPartsCovered.LeftArm, BodyPartType.LeftArm},
		{BodyPartsCovered.RightArm, BodyPartType.RightArm},
		{BodyPartsCovered.Groin, BodyPartType.Groin},
		{BodyPartsCovered.LeftLeg, BodyPartType.LeftLeg},
		{BodyPartsCovered.RightLeg, BodyPartType.RightLeg},
		{BodyPartsCovered.LeftHand, BodyPartType.LeftHand},
		{BodyPartsCovered.RightHand, BodyPartType.RightHand},
		{BodyPartsCovered.LeftFoot, BodyPartType.LeftFoot},
		{BodyPartsCovered.RightFoot, BodyPartType.RightFoot},
	};

}
	 */
}

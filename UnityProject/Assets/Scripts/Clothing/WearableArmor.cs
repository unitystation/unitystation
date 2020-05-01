using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

	private PlayerHealth player;
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
		RightLeft = 1 << 9
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
		{BodyPartsCovered.RightLeft, BodyPartType.RightLeg},
	};

}

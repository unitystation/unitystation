using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// allows clothing to add its armor values to the creature wearing it
/// </summary>
[RequireComponent(typeof(Integrity))]
public class WearableArmor : MonoBehaviour, IServerInventoryMove
{
	[SerializeField]
	[Tooltip("When wore in this slot, the armor values will be applied to player.")]
	private NamedSlot slot;

	[SerializeField]
	[Tooltip("What body parts does this item protect")]
	private BodyPartsCovered bodyPartsCovered;

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
		foreach(BodyPartsCovered flag in bodyPartsCovered.GetFlags())
		{
			if (flag == BodyPartsCovered.none)
			{
				continue;
			}

			BodyPartType bodyPart = bodyParts[flag];
			foreach (BodyPartBehaviour part in player.BodyParts)
			{
				if (part.Type == bodyPart)
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
	}


	[Flags]
	private enum BodyPartsCovered
	{
		none = 0,
		head = 1 << 1,
		eyes = 1 << 2,
		mouth = 1 << 3,
		chest = 1 << 4,
		leftArm = 1 << 5,
		rightArm = 1 << 6,
		groin = 1 << 7,
		leftLeg = 1 << 8,
		rightLeft = 1 << 9
	}

	private readonly Dictionary<BodyPartsCovered, BodyPartType> bodyParts = new Dictionary<BodyPartsCovered, BodyPartType> ()
	{
		{BodyPartsCovered.none, BodyPartType.None},
		{BodyPartsCovered.head, BodyPartType.Head},
		{BodyPartsCovered.eyes, BodyPartType.Eyes},
		{BodyPartsCovered.mouth, BodyPartType.Mouth},
		{BodyPartsCovered.chest, BodyPartType.Chest},
		{BodyPartsCovered.leftArm, BodyPartType.LeftArm},
		{BodyPartsCovered.rightArm, BodyPartType.RightArm},
		{BodyPartsCovered.groin, BodyPartType.Groin},
		{BodyPartsCovered.leftLeg, BodyPartType.LeftLeg},
		{BodyPartsCovered.rightLeft, BodyPartType.RightLeg},
	};

}

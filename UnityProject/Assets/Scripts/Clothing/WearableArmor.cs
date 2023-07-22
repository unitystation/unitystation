using System;
using System.Collections.Generic;
using System.Linq;
using HealthV2;
using UnityEngine;

namespace Clothing
{
	/// <summary>
	/// allows clothing to add its armor values to the creature wearing it
	/// </summary>
	[RequireComponent(typeof(Integrity))]
	public class WearableArmor : MonoBehaviour, IServerInventoryMove
	{
		[SerializeField] [Tooltip("When wore in this slot, the armor values will be applied to player.")]
		private NamedSlot slot = NamedSlot.outerwear;

		[SerializeField]
		private List<NamedSlot> CompatibleSlots = new List<NamedSlot>();


		[SerializeField] [Tooltip("What body parts does this item protect and how well does it protect.")]
		private List<ArmoredBodyPart> armoredBodyParts = new List<ArmoredBodyPart>();
		public List<ArmoredBodyPart> ArmoredBodyParts => armoredBodyParts;

		private PlayerHealthV2 playerHealthV2;


		[Serializable]
		public class ArmoredBodyPart
		{
			[SerializeField] private BodyPartType armoringBodyPartType;

			internal BodyPartType ArmoringBodyPartType => armoringBodyPartType;

			[SerializeField] private Armor armor;

			internal Armor Armor => armor;

			private readonly LinkedList<BodyPart> relatedBodyParts = new LinkedList<BodyPart>();

			public LinkedList<BodyPart> RelatedBodyParts => relatedBodyParts;
		}

		public void OnInventoryMoveServer(InventoryMove info)
		{
			//Wearing
			if (info.ToSlot != null && info.ToSlot.NamedSlot != null)
			{
				playerHealthV2 = info.ToRootPlayer?.PlayerScript.playerHealth;

				if (playerHealthV2 != null && (info.ToSlot.NamedSlot == slot || CompatibleSlots.Contains(info.ToSlot.NamedSlot.Value) ))
				{
					UpdateBodyPartsArmor(false);
				}
			}

			//taking off
			if (info.FromSlot != null && info.FromSlot.NamedSlot != null)
			{
				playerHealthV2 = info.FromRootPlayer?.PlayerScript.playerHealth;

				if (playerHealthV2 != null && (info.FromSlot.NamedSlot == slot || CompatibleSlots.Contains(info.FromSlot.NamedSlot.Value)))
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
			if (currentlyRemovingArmor)
			{
				foreach (ArmoredBodyPart protectedBodyPart in armoredBodyParts)
				{
					while (protectedBodyPart.RelatedBodyParts.Count > 0)
					{
						protectedBodyPart.RelatedBodyParts.First().ClothingArmors.Remove(protectedBodyPart.Armor);
						protectedBodyPart.RelatedBodyParts.RemoveFirst();
					}
				}
				return;
			}

			foreach (ArmoredBodyPart protectedBodyPart in armoredBodyParts)
			{
				foreach (var bodyPart in playerHealthV2.BodyPartList)
				{
					DeepAddArmorToBodyPart(bodyPart, protectedBodyPart);
				}
			}
		}

		/// <summary>
		/// Adds armor per body part depending on the characteristics of this armor.
		/// </summary>
		/// <param name="bodyPart">Body part to update</param>
		/// <param name="armoredBodyPart">A couple of the body part associated with the armor</param>
		private static void DeepAddArmorToBodyPart(BodyPart bodyPart, ArmoredBodyPart armoredBodyPart)
		{
			if (bodyPart.BodyPartType != BodyPartType.None &&
			    bodyPart.BodyPartType == armoredBodyPart.ArmoringBodyPartType)
			{
				bodyPart.ClothingArmors.AddFirst(armoredBodyPart.Armor);
				armoredBodyPart.RelatedBodyParts.AddFirst(bodyPart);
			}
		}
	}
}

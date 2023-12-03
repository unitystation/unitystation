using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HealthV2;
using UI.Systems.Tooltips.HoverTooltips;
using UnityEngine;

namespace Clothing
{
	/// <summary>
	/// allows clothing to add its armor values to the creature wearing it
	/// </summary>
	[RequireComponent(typeof(Integrity))]
	public class WearableArmor : MonoBehaviour, IServerInventoryMove, IHoverTooltip, IExaminable
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

		private string GetInfo()
		{
			StringBuilder text = new StringBuilder();
			var protectedParts = new List<string>();
			foreach (var part in armoredBodyParts)
			{
				if (part.Armor.StunImmunity == false) continue;
				text.AppendLine("This has stun immunity.");
				break;
			}

			foreach (var part in armoredBodyParts)
			{
				protectedParts.Add(part.ArmoringBodyPartType.ToString());
			}

			if (protectedParts.Count != 0)
			{
				if (protectedParts.Count == 1)
				{
					text.AppendLine($"\nThis protects the {protectedParts[0]}.");
				}
				else
				{
					StringBuilder protectedPartsText = new StringBuilder();
					var index = -1;
					foreach (var partText in protectedParts)
					{
						index++;
						if (index == 0)
						{
							protectedPartsText.Append(protectedParts.Count == 1 ? $"the {partText}." : $"the {partText},");
						}
						else if (index == protectedParts.Count - 1)
						{
							protectedPartsText.Append($" and {partText}.");
							break;
						}
						else
						{
							protectedPartsText.Append($" {partText},");
						}
					}
					text.AppendLine($"\nThis protects the {protectedPartsText}");
				}
			}

			var heatProtectionValues = new List<Vector2>();
			foreach (var hPart in armoredBodyParts)
			{
				if (hPart.Armor.TemperatureProtectionInK != Vector2.zero)
				{
					heatProtectionValues.Add(hPart.Armor.TemperatureProtectionInK);
				}
			}

			if (heatProtectionValues.Count != 0)
			{
				float sumFreeze = 0;
				foreach (var number in heatProtectionValues)
				{
					sumFreeze += number.x;
				}
				float sumHeat = 0;
				foreach (var number in heatProtectionValues)
				{
					sumHeat += number.y;
				}
				float averageFreeze = (sumFreeze / heatProtectionValues.Count) - 273.15f;
				float averageHeat = (sumHeat / heatProtectionValues.Count) - 273.15f;

				text.AppendLine($"\nThis has an average heat resistance of <color=red>{averageHeat.ToString("F" + 2)}°C</color> and freeze resistance of <color=#5fcfdd>{averageFreeze.ToString("F" + 2)}°C</color>");
			}
			return text.ToString();
		}

		public string HoverTip()
		{
			if (armoredBodyParts.Count == 0) return null;
			return GetInfo();
		}

		public string CustomTitle()
		{
			return null;
		}

		public Sprite CustomIcon()
		{
			return null;
		}

		public List<Sprite> IconIndicators()
		{
			return null;
		}

		public List<TextColor> InteractionsStrings()
		{
			return null;
		}

		public string Examine(Vector3 worldPos = default(Vector3))
		{
			return armoredBodyParts.Count == 0 ? null : GetInfo();
		}
	}
}

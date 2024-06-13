using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using static System.Collections.Specialized.BitVector32;
using System.Text;
using static UnityEngine.Networking.UnityWebRequest;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Reaction")]
	public class Reaction : ScriptableObject
	{
		public SerializableDictionary<Reagent, int> ingredients;
		public bool useExactAmounts = false;

		public float MinimumReactionMultiple = 0f;

		public SerializableDictionary<Reagent, int> catalysts;
		public SerializableDictionary<Reagent, int> inhibitors;
		[HideInInspector]
		public bool hasMinTemp;
		[HideInInspector]
		public float serializableTempMin;
		[HideInInspector]
		public bool hasMaxTemp;
		[HideInInspector]
		public float serializableTempMax;
		public SerializableDictionary<Reagent, int> results;
		public SerializableDictionary<Effect, int> effectDict;

		public float? tempMin;
		public float? tempMax;

		[SerializeField] private string overrideDisplayName = null;

		private string displayName = null;

		//For ingame GUIs that might need to indentify this reaction. See ExplosiveBountyUIEntry.cs for an example
		public string DisplayName
		{
			get
			{
				if (overrideDisplayName != null) return overrideDisplayName;
				if (displayName != null) return displayName;

				StringBuilder sb = new StringBuilder();

				foreach (KeyValuePair<Reagent, int> product in results.m_dict)
				{
					sb.Append($"{product.Key.Name},");
				}

				sb.Remove(sb.Length - 1, 1); //remove last comma

				displayName = sb.ToString();
				return displayName;
			}
		}

		public virtual bool Apply(object sender, ReagentMix reagentMix)
		{
			if (IsReactionValid(reagentMix) == false) return false;

			ApplyReaction(sender as MonoBehaviour, reagentMix);

			return true;
		}

		public bool IsReactionValid(ReagentMix reagentMix)
		{
			if (HasIngredients(reagentMix) == false)
			{
				return false;
			}

			var reactionMultiple = GetReactionMultiple(reagentMix);

			if (useExactAmounts)
			{
				reactionMultiple = (float)Math.Floor(reactionMultiple);
				if (reactionMultiple == 0)
				{
					return false;
				}
			}

			if (CanReactionHappen(reagentMix, reactionMultiple) == false)
			{
				return false;
			}

			return true;
		}

		public void ApplyReaction(MonoBehaviour sender, ReagentMix reagentMix)
		{
			var reactionMultiplier = GetReactionMultiple(reagentMix);

			foreach (var ingredient in ingredients.m_dict)
			{
				reagentMix.Subtract(ingredient.Key, reactionMultiplier * ingredient.Value);
			}

			foreach (var result in results.m_dict)
			{
				var reactionResult = reactionMultiplier * result.Value;
				reagentMix.Add(result.Key, reactionResult);
			}

			foreach (var effect in effectDict.m_dict)
			{
				var reactionResult = reactionMultiplier * effect.Value;
				effect.Key.Apply(sender, reactionResult);
			}
		}

		public float GetReactionMultiple(ReagentMix reagentMix)
		{
			var reactionMultiplier = Mathf.Infinity;
			foreach (var ingredient in ingredients.m_dict)
			{
				var value = reagentMix.reagents.m_dict[ingredient.Key] / ingredient.Value;
				if (value < reactionMultiplier)
				{
					reactionMultiplier = value;
				}
			}
			return reactionMultiplier;
		}

		/// <summary>
		/// Calculates the total volume of products + quanity of reaction effects produced from this reaction.
		/// Does not perform this reaction.
		/// </summary>
		public float GetReactionQuantity(ReagentMix reagentMix)
		{
			var multiplier = GetReactionMultiple(reagentMix);

			float quantity = 0;

			foreach (var result in results.m_dict)
			{
				quantity += multiplier * result.Value;
			}

			foreach (var effect in effectDict.m_dict)
			{
				quantity += multiplier * effect.Value;
			}

			return quantity;
		}

		public bool HasIngredients(ReagentMix reagentMix)
		{

			//has all ingredients?
			if (ingredients.m_dict.Count == 0)
			{
				return false;
			}

			if (ingredients.m_dict.Count > reagentMix.reagents.m_dict.Count)
			{
				return false;
			}

			foreach (var ingredient in ingredients.m_dict)
			{
				if (reagentMix.reagents.m_dict.ContainsKey(ingredient.Key) == false)
				{
					return false;
				}
				if (ingredient.Value <= 0)
				{
					return false;
				}
			}
			return true;
		}

		public bool CanReactionHappen(ReagentMix reagentMix, float reactionMultiple = 1)
		{
			//correct temperature?
			tempMin = hasMinTemp ? (float?)serializableTempMin : null;
			tempMax = hasMaxTemp ? (float?)serializableTempMax : null;
			if (tempMin != null && reagentMix.Temperature <= tempMin ||
			    tempMax != null && reagentMix.Temperature >= tempMax)
			{
				return false;
			}

			if (MinimumReactionMultiple > reactionMultiple)
			{
				return false;
			}

			//are all catalysts present?
			foreach (var catalyst in catalysts.m_dict)
			{
				if (reagentMix[catalyst.Key] < catalyst.Value * reactionMultiple)
				{
					return false;
				}
			}

			//is a single inhibitor present?
			foreach (var inhibitor in inhibitors.m_dict)
			{
				if (reagentMix[inhibitor.Key] >= inhibitor.Value * reactionMultiple)
				{
					return false;
				}
			}
			return true;
		}

	}
}
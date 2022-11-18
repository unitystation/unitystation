using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

namespace Chemistry
{
	[CreateAssetMenu(fileName = "reaction", menuName = "ScriptableObjects/Chemistry/Reaction")]
	public class Reaction : ScriptableObject
	{
		public SerializableDictionary<Reagent, int> ingredients;
		public bool useExactAmounts = false;
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
		public Effect[] effects;

		public float? tempMin;
		public float? tempMax;

		public virtual bool Apply(MonoBehaviour sender, ReagentMix reagentMix)
		{
			if (IsReactionValid(reagentMix) == false) return false;

			ApplyReaction(sender, reagentMix);

			return true;
		}

		public bool IsReactionValid(ReagentMix reagentMix)
		{
			var reactionAmount = GetReactionAmount(reagentMix);

			if (HasIngredients(reagentMix) == false)
			{
				return false;
			}

			if (useExactAmounts)
			{
				reactionAmount = (float)Math.Floor(reactionAmount);
				if (reactionAmount == 0)
				{
					return false;
				}
			}

			if (CanReactionHappen(reagentMix, reactionAmount) == false)
			{
				return false;
			}

			return true;
		}

		public void ApplyReaction(MonoBehaviour sender, ReagentMix reagentMix)
		{
			var reactionAmount = GetReactionAmount(reagentMix);

			foreach (var ingredient in ingredients.m_dict)
			{
				reagentMix.Subtract(ingredient.Key, reactionAmount * ingredient.Value);
			}

			foreach (var result in results.m_dict)
			{
				var reactionResult = reactionAmount * result.Value;
				reagentMix.Add(result.Key, reactionResult);
			}

			foreach (var effect in effects)
			{
				if (effect != null)
					effect.Apply(sender, reactionAmount);
			}
		}

		public float GetReactionAmount(ReagentMix reagentMix)
		{
			var reactionAmount = Mathf.Infinity;
			foreach (var ingredient in ingredients.m_dict)
			{
				var value = reagentMix.reagents.m_dict[ingredient.Key] / ingredient.Value;
				if (value < reactionAmount)
				{
					reactionAmount = value;
				}
			}
			return reactionAmount;
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

		public bool CanReactionHappen(ReagentMix reagentMix, float reactionAmount = 1)
		{
			//correct temperature?
			tempMin = hasMinTemp ? (float?)serializableTempMin : null;
			tempMax = hasMaxTemp ? (float?)serializableTempMax : null;
			if (tempMin != null && reagentMix.Temperature <= tempMin ||
			    tempMax != null && reagentMix.Temperature >= tempMax)
			{
				return false;
			}

			//are all catalysts present?
			foreach (var catalyst in catalysts.m_dict)
			{
				if (reagentMix[catalyst.Key] < catalyst.Value * reactionAmount)
				{
					return false;
				}
			}

			//is a single inhibitor present?
			foreach (var inhibitor in inhibitors.m_dict)
			{
				if (reagentMix[inhibitor.Key] > inhibitor.Value * reactionAmount)
				{
					return false;
				}
			}
			return true;
		}

	}
}
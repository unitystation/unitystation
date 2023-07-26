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
		public Effect[] effects;

		public float? tempMin;
		public float? tempMax;

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

			var reactionMultiple = GetReactionAmount(reagentMix);

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
			var reactionMultiplier = GetReactionAmount(reagentMix);

			foreach (var ingredient in ingredients.m_dict)
			{
				reagentMix.Subtract(ingredient.Key, reactionMultiplier * ingredient.Value);
			}

			foreach (var result in results.m_dict)
			{
				var reactionResult = reactionMultiplier * result.Value;
				reagentMix.Add(result.Key, reactionResult);
			}

			foreach (var effect in effects)
			{
				if (effect != null)
					effect.Apply(sender, reactionMultiplier);
			}
		}

		public float GetReactionAmount(ReagentMix reagentMix)
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
				if (reagentMix[inhibitor.Key] > inhibitor.Value * reactionMultiple)
				{
					return false;
				}
			}
			return true;
		}

	}
}
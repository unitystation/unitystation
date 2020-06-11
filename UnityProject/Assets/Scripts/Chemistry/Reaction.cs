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
		public DictionaryReagentInt ingredients;
		public bool useExactAmounts = false;
		public DictionaryReagentInt catalysts;
		public float? tempMin;
		public float? tempMax;
		public DictionaryReagentInt results;
		public Effect[] effects;

		public bool Apply(MonoBehaviour sender, ReagentMix reagentMix)
		{
			if ((tempMin != null || reagentMix.Temperature >= tempMin) &&
				(tempMax != null || reagentMix.Temperature <= tempMax))
			{
				return false;
			}

			if (!ingredients.All(reagent => reagentMix[reagent.Key] > 0))
			{
				return false;
			}

			if (!ingredients.Any())
			{
				return false;
			}
			var reactionAmount = ingredients.Min(i => reagentMix[i.Key] / i.Value);

			if (useExactAmounts == true)
			{
				reactionAmount = (float)Math.Floor(reactionAmount);
				if (reactionAmount == 0)
				{
					return false;
				}
			}

			if (!catalysts.All(catalyst =>
				reagentMix[catalyst.Key] > catalyst.Value * reactionAmount))
			{
				return false;
			}

			foreach (var ingredient in ingredients)
			{
				reagentMix.Subtract(ingredient.Key, reactionAmount * ingredient.Value);
			}

			foreach (var result in results)
			{
				var reactionResult = reactionAmount * result.Value;
				reagentMix.Add(result.Key, reactionResult);
			}

			foreach (var effect in effects)
			{
				effect.Apply(sender, reactionAmount);
			}

			return true;
		}
	}
}
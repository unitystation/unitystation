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

			var reagents = reagentMix.reagents;
			if (!ingredients.All(reagent => reagents.TryGetValue(reagent.Key, out var amount) ? amount > 0 : false))
			{
				return false;
			}

			if (!ingredients.Any())
			{
				return false;
			}
			var reactionAmount = ingredients.Min(i => reagents[i.Key] / i.Value);

			if (!catalysts.All(catalyst =>
				reagents.TryGetValue(catalyst.Key, out var amount) && amount > catalyst.Value * reactionAmount))
			{
				return false;
			}

			foreach (var ingredient in ingredients)
			{
				reagents[ingredient.Key] -= reactionAmount * ingredient.Value;
			}

			foreach (var result in results)
			{
				var reactionResult = reactionAmount * result.Value;
				if (reagents.Contains(result.Key))
				{
					reagents[result.Key] += reactionResult;
				}
				else
				{
					reagents[result.Key] = reactionAmount;
				}
			}

			foreach (var effect in effects)
			{
				effect.Apply(sender, reactionAmount);
			}

			return true;
		}
	}
}
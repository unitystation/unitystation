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
		public ReagentMix ingredients;
		public ReagentMix catalysts;
		public float tempMin;
		public float tempMax;
		public ReagentMix results;
		public Effect[] effects;

		public bool Apply(MonoBehaviour sender, Dictionary<Reagent, float> reagents)
		{
			if (!ingredients.All(reagent => reagents.TryGetValue(reagent.Key, out var amount) ? amount > 0 : false))
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
				reagents[result.Key] += reactionAmount * result.Value;
			}

			foreach (var effect in effects)
			{
				effect.Apply(sender, reactionAmount);
			}

			return true;
		}
	}

	[Serializable]
	public class ReagentMix : SerializableDictionary<Reagent, int>
	{
	}
}
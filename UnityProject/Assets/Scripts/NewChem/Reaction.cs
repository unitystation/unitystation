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
		public Reagent[] catalysts;
		public float? tempMin;
		public float? tempMax;
		public ReagentMix results;
		public IEffect[] effects;

		public bool Apply(MonoBehaviour sender, Dictionary<Reagent, float> reagents)
		{
			if (!ingredients.All(i => reagents.TryGetValue(i.Key, out var amount) ? amount > 0 : false) ||
				!catalysts.All(c => reagents.TryGetValue(c, out var amount) ? amount > 0 : false))
			{
				return false;
			}

            var reactionAmount = ingredients.Min(i => reagents[i.Key] / i.Value);

            foreach(var ingredient in ingredients){
                reagents[ingredient.Key] -= reactionAmount * ingredient.Value;
            }

            foreach(var result in results){
                reagents[result.Key] += reactionAmount * result.Value;
            }

			foreach (var effect in effects) effect.Apply(sender, reactionAmount);
            return true;
		}
	}

	[Serializable]
	public class ReagentMix : SerializableDictionary<Reagent, int> { }
}
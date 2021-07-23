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
		public DictionaryReagentInt inhibitors;
		[HideInInspector]
		public bool hasMinTemp;
		[HideInInspector]
		public float serializableTempMin;
		[HideInInspector]
		public bool hasMaxTemp;
		[HideInInspector]
		public float serializableTempMax;
		public DictionaryReagentInt results;
		public Effect[] effects;

		public float? tempMin;
		public float? tempMax;

		public virtual bool Apply(MonoBehaviour sender, ReagentMix reagentMix)
		{
            tempMin = hasMinTemp ? (float?)serializableTempMin : null;
			tempMax = hasMaxTemp ? (float?)serializableTempMax : null;

			if (tempMin != null && reagentMix.Temperature <= tempMin ||
			    tempMax != null && reagentMix.Temperature >= tempMax)
			{
				return false;
			}

			if (!ingredients.m_dict.All(reagent => reagentMix[reagent.Key] > 0))
			{
				return false;
			}

			if (!ingredients.m_dict.Any())
			{
				return false;
			}

			var reactionAmount = ingredients.m_dict.Min(i => reagentMix[i.Key] / i.Value);

			if (useExactAmounts == true)
			{
				reactionAmount = (float) Math.Floor(reactionAmount);
				if (reactionAmount == 0)
				{
					return false;
				}
			}

			if (!catalysts.m_dict.All(catalyst =>
				reagentMix[catalyst.Key] >= catalyst.Value * reactionAmount))
			{
				return false;
			}

			if (inhibitors.m_dict.Count > 0)
			{
				if (inhibitors.m_dict.All(inhibitor => reagentMix[inhibitor.Key] > inhibitor.Value * reactionAmount))
				{
					return false;
				}
			}


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
				effect.Apply(sender, reactionAmount);
			}

			return true;
		}
	}
}
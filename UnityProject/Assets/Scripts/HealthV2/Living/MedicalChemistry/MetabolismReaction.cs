using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Chemistry;
using HealthV2;
using UnityEngine;

public class MetabolismReaction : Reaction
{
	//Should it metabolise faster or slower
	public float ReagentMetabolismMultiplier = 1;
	public override bool Apply(MonoBehaviour sender, ReagentMix reagentMix)
	{
		if (tempMin != null && reagentMix.Temperature <= tempMin ||
		    tempMax != null && reagentMix.Temperature >= tempMax)
		{
			return false;
		}

		if (ingredients.m_dict.Count() == 0)
		{
			return false;
		}

		var cancelApply = true;
		foreach (var ingredient in ingredients.m_dict)
		{
			if (reagentMix.reagents.m_dict.ContainsKey(ingredient.Key) && reagentMix.reagents.m_dict[ingredient.Key] > 0)
			{
				cancelApply = false;
				break;
			}

		}
		if (cancelApply)
		{
			return false;
		}

		var reactionAmount = Mathf.Infinity;
		foreach (var ingredient in ingredients.m_dict)
		{
			var asd = reagentMix.reagents.m_dict[ingredient.Key] / ingredient.Value;
			if (reactionAmount < asd)
			{
				reactionAmount = asd;
			}
		}

		if (useExactAmounts)
		{
			reactionAmount = (float) Math.Floor(reactionAmount);
			if (reactionAmount == 0)
			{
				return false;
			}
		}

		cancelApply = true;
		foreach (var catalyst in catalysts.m_dict)
		{
			if (reagentMix.reagents.m_dict[catalyst.Key] >= catalyst.Value * reactionAmount)
			{
				cancelApply = false;
				break;
			}
		}
		if (cancelApply)
		{
			return false;
		}

		if (inhibitors.m_dict.Count > 0)
		{
			cancelApply = true;
			foreach (var inhibitor in inhibitors.m_dict)
			{
				if (reagentMix.reagents.m_dict[inhibitor.Key] > inhibitor.Value * reactionAmount)
				{
					cancelApply = false;
					break;
				}
			}
			if (cancelApply)
			{
				return false;
			}
		}

		var BodyPart = sender.GetComponent<BodyPart>();
		if (BodyPart == null)
		{
			return false;
		}

		BodyPart.MetabolismReactions.Add(this);

		return true;
	}

	public void React(BodyPart sender, ReagentMix reagentMix, float INreactionAmount)
	{

		if (tempMin != null && reagentMix.Temperature <= tempMin ||
		    tempMax != null && reagentMix.Temperature >= tempMax)
		{
			return;
		}

		if (!ingredients.m_dict.All(reagent => reagentMix.reagents.m_dict[reagent.Key] > 0))
		{
			return;
		}

		if (!ingredients.m_dict.Any())
		{
			return;
		}

		var OptimalAmount = ingredients.m_dict.Min(i => reagentMix.reagents.m_dict[i.Key] / i.Value);

		var reactionAmount = Mathf.Min(OptimalAmount, INreactionAmount*ReagentMetabolismMultiplier);

		if (!catalysts.m_dict.All(catalyst =>
			reagentMix.reagents.m_dict[catalyst.Key] >= catalyst.Value * reactionAmount))
		{
			return;
		}

		if (inhibitors.m_dict.Count > 0)
		{
			if (inhibitors.m_dict.All(inhibitor => reagentMix.reagents.m_dict[inhibitor.Key] > inhibitor.Value * reactionAmount))
			{
				return;
			}
		}

		PossibleReaction(sender, reagentMix, reactionAmount);
	}

	public virtual void PossibleReaction(BodyPart sender, ReagentMix reagentMix, float LimitedreactionAmount)
	{
		foreach (var ingredient in ingredients.m_dict)
		{
			reagentMix.Subtract(ingredient.Key, LimitedreactionAmount * ingredient.Value);
		}

		foreach (var result in results.m_dict)
		{
			var reactionResult = LimitedreactionAmount * result.Value;
			reagentMix.Add(result.Key, reactionResult);
		}

		foreach (var effect in effects)
		{
			effect.Apply(sender, LimitedreactionAmount);
		}
	}

}
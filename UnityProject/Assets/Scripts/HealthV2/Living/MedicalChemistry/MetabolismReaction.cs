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
			reactionAmount = (float) Math.Floor(reactionAmount);
			if (reactionAmount == 0)
			{
				return false;
			}
		}



		if (!catalysts.All(catalyst =>
			reagentMix[catalyst.Key] >= catalyst.Value * reactionAmount))
		{
			return false;
		}

		if (inhibitors.Count > 0)
		{
			if (inhibitors.All(inhibitor => reagentMix[inhibitor.Key] > inhibitor.Value * reactionAmount))
			{
				return false;
			}
		}

		var BodyPart = sender.GetComponent<BodyPart>();

		if (BodyPart == null) return false;

		BodyPart.MetabolismReactions.Add(this);


		return false;
	}

	public virtual void React(BodyPart sender, ReagentMix reagentMix, float INreactionAmount)
	{

		if (tempMin != null && reagentMix.Temperature <= tempMin ||
		    tempMax != null && reagentMix.Temperature >= tempMax)
		{
			return;
		}

		if (!ingredients.All(reagent => reagentMix[reagent.Key] > 0))
		{
			return;
		}

		if (!ingredients.Any())
		{
			return;
		}

		var OptimalAmount = ingredients.Min(i => reagentMix[i.Key] / i.Value);

		var reactionAmount = Mathf.Min(OptimalAmount, INreactionAmount*ReagentMetabolismMultiplier);

		if (!catalysts.All(catalyst =>
			reagentMix[catalyst.Key] >= catalyst.Value * reactionAmount))
		{
			return;
		}

		if (inhibitors.Count > 0)
		{
			if (inhibitors.All(inhibitor => reagentMix[inhibitor.Key] > inhibitor.Value * reactionAmount))
			{
				return;
			}
		}

		PossibleReaction(sender, reagentMix, reactionAmount);
	}

	public virtual void PossibleReaction(BodyPart sender, ReagentMix reagentMix, float LimitedreactionAmount)
	{
		foreach (var ingredient in ingredients)
		{
			reagentMix.Subtract(ingredient.Key, LimitedreactionAmount * ingredient.Value);
		}

		foreach (var result in results)
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
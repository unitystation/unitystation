using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using UnityEngine;

public class MetabolismReaction : Reaction
{
	public float MinimumPercentageThreshold = 0;

	//Should it metabolise faster or slower
	public float ReagentMetabolismMultiplier = 1;

	public override bool Apply(MonoBehaviour sender, ReagentMix reagentMix)
	{
		if (HasIngredients(reagentMix) == false)
		{
			return false;
		}

		if (CanReactionHappen(reagentMix) == false)
		{
			return false;
		}

		var bodyPart = sender.GetComponent<BodyPart>();
		if (bodyPart == null)
		{
			return false;
		}

		bodyPart.MetabolismReactions.Add(this);
		return false;
	}

	public void React(BodyPart sender, ReagentMix reagentMix, float inReactionAmount)
	{
		var reactionAmount = GetReactionAmount(reagentMix);

		if ((reactionAmount / reagentMix.Total) < MinimumPercentageThreshold)
		{
			return;
		}

		PossibleReaction(sender, reagentMix, reactionAmount);
	}

	public virtual void PossibleReaction(BodyPart sender, ReagentMix reagentMix, float limitedreactionAmount)
	{
		foreach (var ingredient in ingredients.m_dict)
		{
			reagentMix.Subtract(ingredient.Key, limitedreactionAmount * ingredient.Value);
		}

		foreach (var result in results.m_dict)
		{
			var reactionResult = limitedreactionAmount * result.Value;
			reagentMix.Add(result.Key, reactionResult);
		}

		foreach (var effect in effects)
		{
			effect.Apply(sender, limitedreactionAmount);
		}
	}
}
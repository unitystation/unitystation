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

	//Reaction.metabolismspeedmultiplier


	public List<ItemTrait> AllRequired = new List<ItemTrait>();
	//public List<ItemTrait> SingleRequired = new List<ItemTrait>(); TODO add ability to Apply to multiple tags
	public List<ItemTrait> Blacklist  = new List<ItemTrait>();

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

		var bodyPart = sender.GetComponent<CirculatorySystemBase>();
		if (bodyPart == null)
		{
			return false;
		}

		bodyPart.MetabolismReactions.Add(this);
		return false;
	}

	public void React(List<BodyPart> sender, ReagentMix reagentMix, float ReactionAmount)
	{
		var reactionPercentage = GetReactionAmount(reagentMix);


		ReactionAmount *=  ReagentMetabolismMultiplier;

		if ((reactionPercentage * reagentMix.Total) > ReactionAmount)
		{
			reactionPercentage = ReactionAmount / reagentMix.Total;
		}

		PossibleReaction(sender, reagentMix, reactionPercentage, ReactionAmount);
	}

	public virtual void PossibleReaction(List<BodyPart> senders, ReagentMix reagentMix, float limitedReactionAmountPercentage, float BodyReactionAmount)
	{
		foreach (var ingredient in ingredients.m_dict)
		{
			reagentMix.Subtract(ingredient.Key, limitedReactionAmountPercentage * ingredient.Value);
		}

		foreach (var result in results.m_dict)
		{
			var reactionResult = limitedReactionAmountPercentage * result.Value;
			reagentMix.Add(result.Key, reactionResult);
		}

		foreach (var effect in effects)
		{
			foreach (var sender in senders)
			{
				effect.Apply(sender, limitedReactionAmountPercentage);
			}

		}
	}
}
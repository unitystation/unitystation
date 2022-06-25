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
		var reactionMultiple = GetReactionAmount(reagentMix);

		ReactionAmount *=  ReagentMetabolismMultiplier;

		var AmountProcessing = 0f;
		foreach (var ingredient in ingredients.m_dict)
		{
			AmountProcessing += (reactionMultiple * ingredient.Value);
		}

		if (AmountProcessing > ReactionAmount)
		{
			reactionMultiple *= (ReactionAmount / AmountProcessing);
		}

		PossibleReaction(sender, reagentMix, reactionMultiple, ReactionAmount);
	}

	public virtual void PossibleReaction(List<BodyPart> senders, ReagentMix reagentMix, float reactionMultiple, float BodyReactionAmount)
	{
		foreach (var ingredient in ingredients.m_dict)
		{
			reagentMix.Subtract(ingredient.Key, reactionMultiple * ingredient.Value);
		}

		foreach (var result in results.m_dict)
		{
			var reactionResult = reactionMultiple * result.Value;
			reagentMix.Add(result.Key, reactionResult);
		}

		foreach (var effect in effects)
		{
			foreach (var sender in senders)
			{
				effect.Apply(sender, reactionMultiple);
			}

		}
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.CirculatorySystem;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
using UnityEngine.Serialization;

public class MetabolismReaction : Reaction
{

	//Should it metabolise faster or slower
	public float ReagentMetabolismMultiplier = 1;

	//Reaction.metabolismspeedmultiplier


	[FormerlySerializedAs("AllRequired")] public List<ItemTrait> InternalAllRequired = new List<ItemTrait>();
	//public List<ItemTrait> SingleRequired = new List<ItemTrait>(); TODO add ability to Apply to multiple tags
	[FormerlySerializedAs("Blacklist")] public List<ItemTrait> InternalBlacklist  = new List<ItemTrait>();


	public List<ItemTrait> ExternalAllRequired = new List<ItemTrait>();
	//public List<ItemTrait> SingleRequired = new List<ItemTrait>(); TODO add ability to Apply to multiple tags
	public List<ItemTrait> ExternalBlacklist  = new List<ItemTrait>();

	public override bool Apply(object sender, ReagentMix reagentMix)
	{
		if (IsReactionValid(reagentMix) == false)
		{
			return false;
		}

		var circulatorySystem = sender as IAreaReactionBase;
		if (circulatorySystem == null)
		{
			return false;
		}

		circulatorySystem.MetabolismReactions.Add(this);

		return false;
	}

	public void React(List<MetabolismComponent> sender, ReagentMix reagentMix, float ReactionAmount)
	{
		var reactionMultiple = GetReactionAmount(reagentMix);

		var AmountProcessing = 0f;
		foreach (var ingredient in ingredients.m_dict)
		{
			AmountProcessing += (ingredient.Value * reactionMultiple);
		}

		if (AmountProcessing > ReactionAmount)
		{
			reactionMultiple *= (ReactionAmount / AmountProcessing);
		}
		//out must be asigned to something, overdose is never used here
		bool overdose;
		PossibleReaction(sender, reagentMix, reactionMultiple, ReactionAmount, AmountProcessing, out overdose);
	}

	public virtual void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix, float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed, out bool overdose)
	{
		//out must be asigned to something, overdose is never used here.
		overdose = false;
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

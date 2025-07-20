using System;
using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.CirculatorySystem;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewMetabolismReaction", menuName = "ScriptableObjects/Chemistry/MetabolismReaction")]
public class MetabolismReaction : Reaction
{

	//Should it metabolise faster or slower
	public float ReagentMetabolismMultiplier = 1;
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

	/// <summary>
	///
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="reagentMix"></param>
	/// <param name="maxReactQuantity">The portion in u of the entire blood pool that should react. (5u means it'll take 100 calls for a given reaction to occur to completion in the blood stream)</param>
	public void React(List<MetabolismComponent> sender, ReagentMix reagentMix, float maxReactQuantity)
	{
		var reactionMultiple = GetReactionMultiple(reagentMix);
		if (reactionMultiple == 0) return;
		reactionMultiple *= (maxReactQuantity / reagentMix.Total);

		var AmountProcessing = 0f;
		foreach (var ingredient in ingredients.m_dict)
		{
			AmountProcessing += (ingredient.Value * reactionMultiple);
		}
		if (AmountProcessing == 0) return;

		//out must be asigned to something, overdose is never used here
		bool overdose = false;

		//Sender - The organs that contain this reaction
		//Reagent Mix - System blood pool
		//Reaction multiple - reaction multiple for the given reaction. Scaled for how much of the blood is processed at once
		//BodyReactionAmount - The max amount of blood that is reacted at once
		//TotalChemicalsProcessed - The amount of ingredients being reacted by this
		PossibleReaction(sender, reagentMix, reactionMultiple, maxReactQuantity, AmountProcessing, ref overdose);
	}

	public virtual void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix, float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed, ref bool overdose)
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

		foreach (var effect in effectDict.m_dict)
		{
			var effectResult = reactionMultiple * effect.Value;

			foreach (var sender in senders)
			{
				effect.Key.Apply(sender, effectResult);
			}
		}
	}
}

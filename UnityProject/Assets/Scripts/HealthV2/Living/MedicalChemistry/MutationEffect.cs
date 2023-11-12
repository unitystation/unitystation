using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;
[CreateAssetMenu(fileName = "BodyHealthEffect",
	menuName = "ScriptableObjects/Chemistry/Reactions/MutationEffect")]
public class MutationEffect : MetabolismReaction
{
	public bool RemoveALLMutations;

	public List<MutationSO> MutationsToAdd = new List<MutationSO>();

	public List<MutationSO> MutationsToRemove = new List<MutationSO>();
	public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float BodyReactionAmount, float TotalChemicalsProcessed,
		out bool overdose) //limitedReactionAmountPercentage = 0 to 1
	{
		foreach (var Processing in senders)
		{
			var Mutation = Processing.RelatedPart.GetComponent<BodyPartMutations>();
			if (Mutation == null) continue;
			if (RemoveALLMutations)
			{
				for (int i = Mutation.ActiveMutations.Count - 1; i >= 0; i--)
				{
					Mutation.RemoveMutation(Mutation.ActiveMutations[i].RelatedMutationSO);
				}
			}
			else
			{
				foreach (var AddMutation in MutationsToAdd)
				{
					Mutation.AddMutation(AddMutation);
				}

				foreach (var removeMutation in MutationsToRemove)
				{
					Mutation.RemoveMutation(removeMutation);
				}
			}
		}


		base.PossibleReaction(senders, reagentMix, reactionMultiple, BodyReactionAmount, TotalChemicalsProcessed,
			out overdose);
	}
}
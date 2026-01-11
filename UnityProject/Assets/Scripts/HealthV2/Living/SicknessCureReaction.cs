using System.Collections.Generic;
using UnityEngine;
using Chemistry;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using Logs;

namespace HealthV2.Sickness
{
	[CreateAssetMenu(fileName = "NewSicknessCureReaction", menuName = "ScriptableObjects/Chemistry/SicknessCureReaction")]
	public class SicknessCureReaction : MetabolismReaction
	{
		// Identical to metabolism reaction but not limited to 5u at a time. As pathogens behave differently, if a cure is limited to 5u it can never
		//Out pace a disease.

		public override void PossibleReaction(List<MetabolismComponent> senders, ReagentMix reagentMix,
		float reactionMultiple, float bodyReactionAmount, float TotalChemicalsProcessed, float UntouchedMultiple, ref bool overdose)
		{
			float toReact = Mathf.Min(UntouchedMultiple, 1);

			//out must be asigned to something, overdose is never used here.
			overdose = false;
			foreach (var ingredient in ingredients.m_dict)
			{
				reagentMix.Subtract(ingredient.Key, toReact * ingredient.Value);
			}

			foreach (var result in results.m_dict)
			{
				var reactionResult = toReact * result.Value;
				reagentMix.Add(result.Key, reactionResult);
			}

			foreach (var effect in effectDict.m_dict)
			{
				var effectResult = toReact * effect.Value;

				foreach (var sender in senders)
				{
					effect.Key.Apply(sender,reagentMix ,sender.gameObject.AssumedWorldPosServer() ,   effectResult);
				}
			}
		}
	}
}


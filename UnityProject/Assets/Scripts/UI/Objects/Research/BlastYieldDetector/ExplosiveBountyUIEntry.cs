using UI.Core.NetUI;
using UnityEngine;
using Systems.Research;
using System.Collections.Generic;
using Chemistry;

namespace UI.Objects.Research
{
	public class ExplosiveBountyUIEntry : DynamicEntry
	{
		[SerializeField] private NetText_label bountyDetails;
		[SerializeField] private NetText_label bountyName;

		public void Initialise(ExplosiveBounty bountyData, int index)
		{
			string label_text = $"Required Yield: {bountyData.RequiredYield.requiredAmount}\nRequired Reactions:";
			foreach (ReactionBountyEntry reaction in bountyData.RequiredReactions)
			{
				foreach (KeyValuePair<Reagent, int> product in reaction.requiredReaction.results.m_dict)
				{
					label_text += $"\n\t-{product.Key.Name}: {reaction.requiredAmount}u";
				}
			}

			label_text += $"\nRequired Reagents:";

			foreach (ReagentBountyEntry reagent in bountyData.RequiredReagents)
			{
				label_text += $"\n\t-{reagent.requiredReagent.Name}: {reagent.requiredAmount}u";			
			}

			bountyDetails.MasterSetValue(label_text);
			bountyName.MasterSetValue($"Target [{index}]");
		}
	}
}

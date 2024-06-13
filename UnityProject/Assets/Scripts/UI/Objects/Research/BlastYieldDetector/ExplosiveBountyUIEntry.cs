using UI.Core.NetUI;
using UnityEngine;
using Systems.Research;
using System.Collections.Generic;
using Chemistry;
using System.Text;

namespace UI.Objects.Research
{
	public class ExplosiveBountyUIEntry : DynamicEntry
	{
		[SerializeField] private NetText_label bountyDetails;
		[SerializeField] private NetText_label bountyName;

		public void Initialise(ExplosiveBounty bountyData, int index)
		{
			StringBuilder label_text = new StringBuilder($"Required Yield: {bountyData.RequiredYield.RequiredAmount}\nRequired Reactions:");

			foreach (ReactionBountyEntry reaction in bountyData.RequiredReactions)
			{
				label_text.Append($"\n\t-{reaction.RequiredReaction.DisplayName}: {reaction.RequiredAmount}u");
			}

			label_text.Append($"\nRequired Reagents:");

			foreach (ReagentBountyEntry reagent in bountyData.RequiredReagents)
			{
				label_text.Append($"\n\t-{reagent.RequiredReagent.Name}: {reagent.RequiredAmount}u");			
			}

			bountyDetails.SetValue(label_text.ToString());

			string targetName = bountyData.BountyName != null ? bountyData.BountyName : index.ToString();
			bountyName.MasterSetValue($"Target [{targetName}]");
		}
	}
}

using System.Text;
using UnityEngine;
using US13.ScriptableObjects.Research.Ordnance;
using US13.UI.Core.Net.Elements;
using US13.UI.Core.Net.Elements.Dynamic;

namespace US13.UI.Objects.Research.BlastYieldDetector
{
	public class ExplosiveBountyUIEntry : DynamicEntry
	{
		[SerializeField] private NetText_label bountyDetails;
		[SerializeField] private NetText_label bountyName;

		public void Initialise(ExplosiveBounty bountyData, int index)
		{
			StringBuilder label_text = new StringBuilder($"Required Yield: {bountyData.RequiredYield.RequiredAmount}\nRequired Effects:");

			foreach (EffectBountyEntry effect in bountyData.RequiredEffects)
			{
				label_text.Append($"\n\t-{effect.RequiredEffect.DisplayName}: {effect.RequiredAmount}u");
			}

			label_text.Append($"\nRequired Reagents:");

			foreach (ReagentBountyEntry reagent in bountyData.RequiredReagents)
			{
				label_text.Append($"\n\t-{reagent.RequiredReagent.Name}: {reagent.RequiredAmount}u");
			}

			bountyDetails.MasterSetValue(label_text.ToString());

			string targetName = bountyData.BountyName != null && bountyData.BountyName != "" ? bountyData.BountyName : index.ToString();
			bountyName.MasterSetValue($"Target [{targetName}]");
		}
	}
}

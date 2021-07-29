using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;

namespace HealthV2
{
	public class Kidneys : Organ
	{
		public List<Reagent> WhiteListReagents = new List<Reagent>();

		public Dictionary<Reagent, float> ContainedGoodReagents = new Dictionary<Reagent, float>();

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			if(WhiteListReagents.Count == 0)
			{
				WhiteListReagents.Add(RelatedPart.bloodType);
				WhiteListReagents.Add(RelatedPart.requiredReagent);
				WhiteListReagents.Add(RelatedPart.wasteReagent);
				WhiteListReagents.Add(RelatedPart.Nutriment);
			}
		}

		public override void ImplantPeriodicUpdate()
		{
			base.ImplantPeriodicUpdate();
			ContainedGoodReagents.Clear();
			foreach (var Reagent in WhiteListReagents)
			{
				ContainedGoodReagents[Reagent] = RelatedPart.BloodContainer[Reagent];
			}
			RelatedPart.BloodContainer.CurrentReagentMix.Clear();

			foreach (var Reagents in ContainedGoodReagents)
			{
				RelatedPart.BloodContainer.CurrentReagentMix.Add(Reagents.Key, Reagents.Value);
			}
			//Debug.Log("Kidney: " + BloodContainer[requiredReagent]/bloodType.GetGasCapacity(BloodContainer.CurrentReagentMix));
		}
	}
}

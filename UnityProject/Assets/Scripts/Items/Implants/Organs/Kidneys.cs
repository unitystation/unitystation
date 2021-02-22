using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;

namespace HealthV2
{
	public class Kidneys : BodyPart
	{
		public List<Reagent> WhiteListReagents = new List<Reagent>();

		public Dictionary<Reagent, float> ContainedGoodReagents = new Dictionary<Reagent, float>();

		public override void BloodUpdate()
		{
			base.BloodUpdate();
			ContainedGoodReagents.Clear();
			foreach (var Reagent in WhiteListReagents)
			{
				ContainedGoodReagents[Reagent] = BloodContainer[Reagent];
			}
			BloodContainer.CurrentReagentMix.Clear();

			foreach (var Reagents in ContainedGoodReagents)
			{
				BloodContainer.CurrentReagentMix.Add(Reagents.Key, Reagents.Value);
			}
		}


	}
}

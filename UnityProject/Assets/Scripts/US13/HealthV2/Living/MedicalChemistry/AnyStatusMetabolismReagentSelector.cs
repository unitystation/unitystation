using System;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;

namespace US13.HealthV2.Living.MedicalChemistry
{
	[Serializable]
	public class AnyStatusMetabolismReagentSelector : IStatusMetabolismReagentSelector
	{
		public List<Reagent> Reagents = new();

		public bool HasMatch(ReagentMix reagentMix)
		{
			if (reagentMix == null) return false;
			foreach (var reagent in Reagents)
			{
				if (reagent != null && reagentMix[reagent] > 0) return true;
			}
			return false;
		}

		public void ConsumeMatches(ReagentMix reagentMix, float maxReactQuantity, float metabolismMultiplier)
		{
			if (reagentMix == null || reagentMix.Total == 0) return;
			var reactionScale = maxReactQuantity / reagentMix.Total;
			foreach (var reagent in Reagents)
			{
				if (reagent == null) continue;
				var untouchedMultiple = reagentMix[reagent];
				if (untouchedMultiple <= 0) continue;
				var reactionMultiple = Mathf.Min(untouchedMultiple * reactionScale * metabolismMultiplier, untouchedMultiple);
				reagentMix.Subtract(reagent, reactionMultiple);
			}
		}
	}
}

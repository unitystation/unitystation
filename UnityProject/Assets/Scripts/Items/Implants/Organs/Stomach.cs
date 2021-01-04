using System.Collections;
using System.Collections.Generic;
using Chemistry;
using UnityEngine;
using Chemistry.Components;

namespace HealthV2
{
	public class Stomach : BodyPart
	{
		public ReagentContainer StomachContents = null;

		public Reagent Nutriment;

		public float DigesterAmountPerSecond = 1;

		public override void ImplantPeriodicUpdate(LivingHealthMasterBase healthMaster)
		{
			base.ImplantPeriodicUpdate(healthMaster);
			if (StomachContents[Nutriment] > 0)
			{
				float ToDigest = DigesterAmountPerSecond * TotalModified;
				if (StomachContents[Nutriment] < ToDigest)
				{
					ToDigest = StomachContents[Nutriment];
				}
				var Digesting = StomachContents.TakeReagents(ToDigest);

				healthMaster.NutrimentLevel += Digesting[Nutriment];
				//What to do with non Digesting content, put back in stomach?

			}
		}
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Bones : BodyPart
	{
		public float BloodGeneratedByOneNutriment = 1;
		public BloodType GeneratesThis;

		public override void BloodUpdate()
		{
			if (bloodReagentStoredMax > BloodContainer.ReagentMixTotal && BloodContainer[Nutriment] > 0)
			{
				float toConsume = NutrimentConsumption;
				if (NutrimentConsumption > BloodContainer[Nutriment])
				{
					toConsume = BloodContainer[Nutriment];
				}

				BloodContainer.CurrentReagentMix.Remove(Nutriment,toConsume );
				BloodContainer.CurrentReagentMix.Add(GeneratesThis.Blood, BloodGeneratedByOneNutriment * toConsume);
				NutrimentHeal(toConsume);
			}

			base.BloodUpdate();

		}
	}
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Bones : BodyPart
	{
		[SerializeField] private float BloodGeneratedByOneNutriment = 1;
		[SerializeField] private BloodType GeneratesThis;
		public override void SetUpSystems()
		{
			base.SetUpSystems();
			if(GeneratesThis == null)
			{
				GeneratesThis = healthMaster.CirculatorySystem.BloodType;
			}
		}
		protected override void ConsumeNutriments()
		{
			if (bloodStoredMax > BloodContainer.ReagentMixTotal && BloodContainer[Nutriment] > 0)
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
			base.ConsumeNutriments();
		}
	}
}
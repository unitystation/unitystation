using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Bones : Organ
	{
		[SerializeField] private float BloodGeneratedByOneNutriment = 1;
		[SerializeField] private BloodType GeneratesThis;
		public override void SetUpSystems()
		{
			base.SetUpSystems();
			if (GeneratesThis == null)
			{
				GeneratesThis = RelatedPart.HealthMaster.CirculatorySystem.BloodType;
			}
		}
		public override void ImplantPeriodicUpdate()
		{
			if (RelatedPart.BloodStoredMax > RelatedPart.BloodContainer.ReagentMixTotal && RelatedPart.BloodContainer[RelatedPart.Nutriment] > 0 &&
			    RelatedPart.HealthMaster.GetTotalBlood() < RelatedPart.HealthMaster.CirculatorySystem.BloodInfo.BLOOD_NORMAL)
			{
				float toConsume = RelatedPart.ConsumptionNutriment * RelatedPart.BloodThroughput;
				if (toConsume > RelatedPart.BloodContainer[RelatedPart.Nutriment])
				{
					toConsume = RelatedPart.BloodContainer[RelatedPart.Nutriment];
				}

				RelatedPart.BloodContainer.CurrentReagentMix.Remove(RelatedPart.Nutriment, toConsume);
				RelatedPart.BloodContainer.CurrentReagentMix.Add(GeneratesThis, BloodGeneratedByOneNutriment * toConsume);
			}
		}
	}
}
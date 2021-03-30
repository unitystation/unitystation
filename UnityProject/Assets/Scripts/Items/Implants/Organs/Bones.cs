﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HealthV2
{
	public class Bones : BodyPartModification
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
			    RelatedPart.HealthMaster.GetTotalBlood() < RelatedPart.HealthMaster.CirculatorySystem.BloodInfo.BLOOD_NORMAL / 1000)
			{
				float toConsume = RelatedPart.ConsumptionNutriment;
				if (RelatedPart.ConsumptionNutriment > RelatedPart.BloodContainer[RelatedPart.Nutriment])
				{
					toConsume = RelatedPart.BloodContainer[RelatedPart.Nutriment];
				}

				RelatedPart.BloodContainer.CurrentReagentMix.Remove(RelatedPart.Nutriment, toConsume);
				RelatedPart.BloodContainer.CurrentReagentMix.Add(GeneratesThis, BloodGeneratedByOneNutriment * toConsume);
			}
		}
	}
}
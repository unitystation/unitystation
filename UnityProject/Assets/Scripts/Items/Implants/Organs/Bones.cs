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
			if (GeneratesThis == null)
			{
				GeneratesThis = healthMaster.CirculatorySystem.BloodType;
			}
		}
		protected override void BloodUpdate()
		{
			if (BloodStoredMax > BloodContainer.ReagentMixTotal && BloodContainer[Nutriment] > 0 &&
				healthMaster.GetTotalBlood() < healthMaster.CirculatorySystem.BloodInfo.BLOOD_NORMAL / 1000)
			{
				float toConsume = NutrimentConsumption;
				if (NutrimentConsumption > BloodContainer[Nutriment])
				{
					toConsume = BloodContainer[Nutriment];
				}

				BloodContainer.CurrentReagentMix.Remove(Nutriment, toConsume);
				BloodContainer.CurrentReagentMix.Add(GeneratesThis, BloodGeneratedByOneNutriment * toConsume);
			}
			base.BloodUpdate();
		}
	}
}
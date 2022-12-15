using HealthV2;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Bones : BodyPartFunctionality
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
			var Bloodavailability = GeneratesThis.CalculatePercentageBloodPresent(RelatedPart.HealthMaster.CirculatorySystem.BloodPool);

			if (RelatedPart.HealthMaster.CirculatorySystem.StartingBlood > RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Total  //Assuming this is blood cap max
			    && Bloodavailability < 1f)
			{
				float toConsume = RelatedPart.PassiveConsumptionNutriment * RelatedPart.HealingNutrimentMultiplier;
				if (toConsume > RelatedPart.HealthMaster.CirculatorySystem.BloodPool[RelatedPart.Nutriment])
				{
					toConsume = RelatedPart.HealthMaster.CirculatorySystem.BloodPool[RelatedPart.Nutriment];
				}

				RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Remove(RelatedPart.Nutriment, toConsume);
				RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Add(GeneratesThis, BloodGeneratedByOneNutriment * toConsume);
			}
		}
	}
}
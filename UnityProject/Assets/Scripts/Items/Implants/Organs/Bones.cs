using HealthV2;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Bones : BodyPartFunctionality
	{
		[SerializeField] public float BloodGeneratedByOneNutriment = 30;
		[SerializeField] private BloodType GeneratesThis;

		public float GenerationOvershoot = 1;

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			if (GeneratesThis == null)
			{
				GeneratesThis = RelatedPart.HealthMaster.CirculatorySystem.BloodType;
			}
		} //TODO remove

		public override void ImplantPeriodicUpdate()
		{
			if ((RelatedPart.HealthMaster.CirculatorySystem.StartingBlood * GenerationOvershoot) > RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Total)  //Assuming this is blood cap max)
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
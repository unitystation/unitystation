using HealthV2;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Bones : BodyPartFunctionality
	{
		[SerializeField] private float hungerUsedNormal;
		[SerializeField] public float BloodGeneratedByOneHunger = 30;
		[SerializeField] private BloodType GeneratesThis;

		public float GenerationOvershoot = 1;


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
			if ((RelatedPart.HealthMaster.CirculatorySystem.StartingBlood * GenerationOvershoot) > RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Total)  //Assuming this is blood cap max)
			{
				RelatedPart.HungerConsumption = hungerUsedNormal * RelatedPart.HealingNutrimentMultiplier;
				RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Add(GeneratesThis, BloodGeneratedByOneHunger * RelatedPart.HungerConsumption);
			}
		}
	}
}
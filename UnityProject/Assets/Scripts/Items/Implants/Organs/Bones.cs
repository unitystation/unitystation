using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace Items.Implants.Organs
{
	public class Bones : BodyPartFunctionality
	{
		[SerializeField] public float BloodGeneratedByOneNutriment = 30;
		[SerializeField] private Reagent GeneratesThis;

		public float GenerationOvershoot = 1;

		public HungerComponent HungerComponent;

		public ReagentCirculatedComponent ReagentCirculatedComponent;

		public override void Awake()
		{
			base.Awake();
			HungerComponent = this.GetComponentCustom<HungerComponent>();
			ReagentCirculatedComponent = this.GetComponentCustom<ReagentCirculatedComponent>();
		}

		public override void SetUpSystems()
		{
			base.SetUpSystems();
			if (GeneratesThis == null)
			{
				GeneratesThis = ReagentCirculatedComponent.bloodReagent;
			}
		}

		public override void ImplantPeriodicUpdate()
		{
			if ((ReagentCirculatedComponent.AssociatedSystem.StartingBlood * GenerationOvershoot) > ReagentCirculatedComponent.AssociatedSystem.BloodPool.Total)  //Assuming this is blood cap max)
			{
				float toConsume = HungerComponent.PassiveConsumptionNutriment * HungerComponent.HealingNutrimentMultiplier;
				if (toConsume > ReagentCirculatedComponent.AssociatedSystem.BloodPool[HungerComponent.Nutriment])
				{
					toConsume = ReagentCirculatedComponent.AssociatedSystem.BloodPool[HungerComponent.Nutriment];
				}

				ReagentCirculatedComponent.AssociatedSystem.BloodPool.Remove(HungerComponent.Nutriment, toConsume);
				ReagentCirculatedComponent.AssociatedSystem.BloodPool.Add(GeneratesThis, BloodGeneratedByOneNutriment * toConsume);
			}
		}
	}
}
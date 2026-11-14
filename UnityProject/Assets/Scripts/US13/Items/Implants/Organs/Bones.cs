using Chemistry;
using UnityEngine;
using US13.HealthV2.Living;
using US13.HealthV2.Living.PolymorphicSystems.Bodypart;
using Util;

namespace US13.Items.Implants.Organs
{
	[System.Serializable]
	public class Bones : BodyPartBehavior
	{
		[SerializeField] public float BloodGeneratedByOneNutriment = 30;
		[SerializeField] private Reagent GeneratesThis;

		public float GenerationOvershoot = 1;

		[UnityEngine.SerializeReference] public HungerComponent HungerComponent;

		[UnityEngine.SerializeReference] public ReagentCirculatedComponent ReagentCirculatedComponent;

		public override void OnInitialize()
		{
			base.OnInitialize();
			HungerComponent = GetBodyPartBehavior<HungerComponent>();
			ReagentCirculatedComponent = GetBodyPartBehavior<ReagentCirculatedComponent>();
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
			if (ReagentCirculatedComponent?.AssociatedSystem == null || HungerComponent == null || GeneratesThis == null) return;
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

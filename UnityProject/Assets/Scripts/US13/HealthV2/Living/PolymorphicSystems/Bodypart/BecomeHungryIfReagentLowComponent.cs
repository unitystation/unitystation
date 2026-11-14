using US13.HealthV2.Living;
using Chemistry;
using UnityEngine;
using US13.HealthV2.Living.Metabolism;
using Util;

namespace US13.HealthV2.Living.PolymorphicSystems.Bodypart
{


	[System.Serializable]

	public class BecomeHungryIfReagentLowComponent : BodyPartBehavior
	{
		[UnityEngine.SerializeReference] private HungerComponent HungerComponent;
		public Reagent ScanningFor;
		public float TriggerHungerAtLevel = 55;


		public override void OnInitialize()
		{
			base.OnInitialize();
			HungerComponent = GetBodyPartBehavior<HungerComponent>();
		}


		public override void ImplantPeriodicUpdate()
		{
			var pool = RelatedPart.HealthMaster.GetSystem<ReagentPoolSystem>();

			if (pool.BloodPool[ScanningFor] < TriggerHungerAtLevel)
			{
				HungerComponent.HungerState = HungerState.Hungry;
				return;
			}
		}
	}
}



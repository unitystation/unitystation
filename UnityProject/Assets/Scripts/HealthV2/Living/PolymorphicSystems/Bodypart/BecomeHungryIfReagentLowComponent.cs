using System.Collections;
using System.Collections.Generic;
using Chemistry;
using HealthV2;
using HealthV2.Living.PolymorphicSystems;
using HealthV2.Living.PolymorphicSystems.Bodypart;
using UnityEngine;

namespace HealthV2
{
	[RequireComponent(typeof(HungerComponent))]
	[RequireComponent(typeof(ReagentCirculatedComponent))]
	public class BecomeHungryIfReagentLowComponent : BodyPartFunctionality
	{
		private HungerComponent HungerComponent;
		public Reagent ScanningFor;
		public float TriggerHungerAtLevel = 55;


		public override void Awake()
		{
			base.Awake();
			HungerComponent = this.GetComponentCustom<HungerComponent>();
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



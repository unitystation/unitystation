using UnityEngine;
using HealthV2;
using Chemistry;

namespace Items.Implants.Organs
{
	public class NutrientPump : BodyPartFunctionality
	{
		[SerializeField] private HungerState HungerState = HungerState.Hungry;
		[SerializeField] private ReagentMix toxinMix;

		public override void ImplantPeriodicUpdate()
		{
			if (RelatedPart.HealthMaster.DigestiveSystem == null) return;

			RelatedPart.HealthMaster.DigestiveSystem.ClampHunger(HungerState);
		}

		public override void EmpResult(int strength)
		{
			RelatedPart.HealthMaster.CirculatorySystem.BloodPool.Add(toxinMix);
		}
	}
}

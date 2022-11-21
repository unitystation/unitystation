using HealthV2;
using UnityEngine;

namespace Items.Weapons
{
	public class ImplantExplosiveTrigger : BodyPartFunctionality
	{
		[field: SerializeField] public bool TriggerOnDeath { get; private set; } = false;

		[field: SerializeField] public int OverrideDeathCountDown { get; private set; } = -1;

		bool hasDetonated = false;

		private ImplantExplosive explosive;

		public override void SetUpSystems()
		{
			explosive = GetComponent<ImplantExplosive>();
		}

		public override void ImplantPeriodicUpdate()
		{
			if (explosive == null) return;

			if (RelatedPart.HealthMaster == null) return;

			if(RelatedPart.HealthMaster.IsDead && TriggerOnDeath && hasDetonated == false) //Makes sure bombs dont double detonate
			{
				Detonate();
			}
		}

		private void Detonate()
		{
			hasDetonated = true;
			if (OverrideDeathCountDown >= 0) explosive.TimeToDetonate = OverrideDeathCountDown;

			StartCoroutine(explosive.Countdown());
		}

		public override void EmpResult(int strength)
		{
			Detonate();
		}
	}
}

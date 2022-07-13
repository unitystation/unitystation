using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using HealthV2;

namespace Items.Weapons
{
	public class ImplantExplosiveTrigger : BodyPartFunctionality
	{
		public bool TriggerOnDeath = false;
		bool hasDetonated = false;

		private ImplantExplosive explosive;

		public override void SetUpSystems()
		{
			explosive = GetComponent<ImplantExplosive>();
		}

		public override void ImplantPeriodicUpdate()
		{
			if (explosive == null) return;

			if(RelatedPart.HealthMaster.IsDead && TriggerOnDeath && hasDetonated == false) //Makes sure bombs dont double detonate
			{
				hasDetonated = true;
				StartCoroutine(explosive.Countdown());
			}
		}
	}
}

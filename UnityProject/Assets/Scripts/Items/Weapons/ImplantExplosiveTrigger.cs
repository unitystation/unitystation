using System.Collections;
using HealthV2;
using NaughtyAttributes;
using UnityEngine;

namespace Items.Weapons
{
	public class ImplantExplosiveTrigger : BodyPartFunctionality
	{
		[field: SerializeField] public bool TriggerOnDeath { get; private set; } = false;

		[field: SerializeField] public int OverrideDeathCountDown { get; private set; } = -1;

		bool hasDetonated = false;

		private ImplantExplosive explosive;

		public bool isEMPVunerable = false;

		[ShowIf("isEMPVunerable")]
		public int EMPResistance = 2;

		private IEnumerator ExplodingCoroutine = null;

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

			if (ExplodingCoroutine != null && RelatedPart.HealthMaster.IsDead == false)
			{
				StartCoroutine(ExplodingCoroutine);
				ExplodingCoroutine = null;
			}
		}

		private void Detonate()
		{
			hasDetonated = true;
			if (OverrideDeathCountDown >= 0) explosive.TimeToDetonate = OverrideDeathCountDown;
			ExplodingCoroutine = explosive.Countdown();
			StartCoroutine(ExplodingCoroutine);
		}

		public override void OnEmp(int strength)
		{
			if (isEMPVunerable == false) return;

			if (EMPResistance == 0 || DMMath.Prob(100 / EMPResistance))
			{
				Detonate();
			}

		}
	}
}

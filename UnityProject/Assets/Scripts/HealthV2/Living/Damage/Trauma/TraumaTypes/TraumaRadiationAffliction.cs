using Logs;
using UnityEngine;

namespace HealthV2.TraumaTypes
{
	public class TraumaRadiationAfflict : TraumaLogic
	{
		[SerializeField,Tooltip("The radiation damage dealt is reduced by this factor to calculate added pathogen count.")]
		private float afflictionReductionFactor = 4f;

		private const float DeadlyCancerCount = 25f;

		[SerializeField]
		private float MinThresholdDamage = 0.2f;

		public override void OnTakeDamage(BodyPartDamageData data)
		{
			if (data.TramuticDamageType.HasFlag(traumaTypes) == false) return;
			if (data.AttackType != AttackType.Internal) return; //Processing of radiation stacks is internal damage.
			if (DMMath.Prob(GetRadProtectionPercentage())) return;
			if (DMMath.Prob(data.TraumaDamageChance) == false) return;
			if (data.DamageAmount < MinThresholdDamage) return;
			float overflowDamage = data.DamageAmount - MinThresholdDamage;

			if (overflowDamage > deadlyDamageInOneHit)
			{
				ProgressDeadlyEffect();
				return;
			}
			AfflictMinorCancer(overflowDamage);
		}

		private void AfflictMinorCancer(float damageAmount)
		{
			bodyPart.HealthMaster.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.SpaceCancerReagent, damageAmount / afflictionReductionFactor);
		}

		public override void ProgressDeadlyEffect()
		{
			bodyPart.HealthMaster.reagentPoolSystem.BloodPool.Add(CommonSicknesses.Instance.SpaceCancerReagent, DeadlyCancerCount);
		}

		private float GetRadProtectionPercentage()
		{
			var percent = 0f;
			foreach (var armor in bodyPart.ClothingArmors)
			{
				percent += armor.Rad;
			}
			return percent;
		}
	}
}